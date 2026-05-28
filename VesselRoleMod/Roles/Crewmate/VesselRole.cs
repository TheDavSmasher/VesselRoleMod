using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities.Extensions;
using Rewired.Utils.Classes.Data;
using System;
using System.Linq;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Buttons.Crewmate;
using VesselRoleMod.Buttons.Modifiers;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.Components;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Patches.ControlSystem;
using VesselRoleMod.Utilities;
using Object = UnityEngine.Object;

namespace VesselRoleMod.Roles.Crewmate;

public sealed class VesselRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
	public override bool IsAffectedByComms => false;

	public DoomableType DoomHintType => DoomableType.Death;
	public string RoleName => TouLocale.Get("VesselRole");
	public string RoleDescription => TouLocale.GetParsed("VesselRoleIntroBlurb");
	public string RoleLongDescription => TouLocale.GetParsed("VesselRoleTabDescription");

	public string GetAdvancedDescription()
	{
		return
			TouLocale.GetParsed("VesselRoleWikiDescription") +
			MiscUtils.AppendOptionsText(GetType());
	}

	public Color RoleColor => VesselRoleModColors.Vessel;
	public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
	public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;


	public CustomRoleConfiguration Configuration => new(this)
	{
		Icon = VesselRoleIcons.Vessel,
		IntroSound = TouAudio.MediumIntroSound
	};

	public override void Initialize(PlayerControl player)
	{
		RoleBehaviourStubs.Initialize(this, player);

		if (OptionGroupSingleton<VesselOptions>.Instance.CanRejectPossession != VesselRejectionType.None && 
			!player.HasModifier<VesselBlacklistModifier>())
		{
			player.AddModifier<VesselBlacklistModifier>();
		}
	}

	public override void Deinitialize(PlayerControl targetPlayer)
	{
		RoleBehaviourStubs.Deinitialize(this, targetPlayer);
		if (targetPlayer.HasModifier<VesselBlacklistModifier>())
		{
			targetPlayer.RemoveModifier<VesselBlacklistModifier>();
		}
	}

	public override void OnMeetingStart()
	{
		RoleBehaviourStubs.OnMeetingStart(this);
	}

	[MethodRpc((uint)VesselModRpc.AdorcismStart)]
	public static void RpcSeekVessel(PlayerControl player, PlayerControl target)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(player);
			return;
		}
		if (target.Data.Role is not VesselRole)
		{
			Error($"RpcSeekVessel - Invalid Vessel target");
			return;
		}

		if (!player.HasModifier<ValidAdorcismGhostModifier>(x => x.Vessel.PlayerId == target.PlayerId))
		{
			player.AddModifier<ValidAdorcismGhostModifier>(target);
		}

		var color = Palette.PlayerColors[target.GetDefaultAppearance().ColorId];
		if (player.AmOwner)
		{
			var mod = new PoltergeistArrowModifier(target, color);
			player.AddModifier(mod);

			CustomButtonSingleton<PoltergeistPossessButton>.Instance.SetActive(true, player.Data.Role);
		}
	}

	public static void VesselClosed(PlayerControl player, PlayerControl target)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(player);
			return;
		}
		if (target.Data.Role is not VesselRole)
		{
			Error($"RpcVesselClosed - Invalid Vessel target");
			return;
		}

		if (player.TryGetModifier<ValidAdorcismGhostModifier>(out var mod, x => x.Vessel.PlayerId == target.PlayerId))
		{
			player.RemoveModifier(mod);
		}
		else
		{
			Error($"RpcVesselClosed - Invalid ghost");
		}

		if (player.AmOwner)
		{
			if (!player.HasModifier<VesselSeekingModifier>())
			{
				CustomButtonSingleton<PoltergeistPossessButton>.Instance.SetActive(false, player.Data.Role);
			}

			if (player.GetModifier<PoltergeistArrowModifier>(m => m.Owner.PlayerId == target.PlayerId) is { } arrow)
			{
				player.RemoveModifier(arrow);
			}
		}
	}

	[MethodRpc((uint)VesselModRpc.VesselTryPossessing)]
	public static void RpcGhostTryPossessing(PlayerControl ghost, PlayerControl vessel)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}
		if (ghost.GetModifier<ValidAdorcismGhostModifier>(x => x.Vessel.PlayerId == vessel.PlayerId) is not { } mod)
		{
			Error($"RpcPossess - Invalid poltergeist");
			return;
		}
		if (vessel == null || vessel.Data == null || vessel.HasDied())
		{
			return;
		}
		if (vessel.Data.Role is not VesselRole)
		{
			Error("RpcPossess - Invalid Vessel target");
			return;
		}

		if (vessel.IsInTargetingAnimState())
		{
			return;
		}

		if (OptionGroupSingleton<VesselOptions>.Instance.CanRejectPossession != VesselRejectionType.Free)
		{
			GhostPossession(ghost, vessel);
			return;
		}

		VesselControlState.SetTimerPaused(vessel.PlayerId);

		if (vessel.AmOwner)
		{
			var confirmMenu = VesselConfirmMinigame.Create();
			confirmMenu.Open(
				ghost.PlayerId,
				ghost.Data.PlayerName,
				confirmation =>
				{
					RpcGhostPossession(ghost, vessel, confirmation);
					confirmMenu.Close();
				}
			);
		}
		else if (ghost.AmOwner)
		{
			mod.CreateNotification();
		}
	}

	[MethodRpc((uint)VesselModRpc.VesselPossession)]
	private static void RpcGhostPossession(PlayerControl ghost, PlayerControl vessel, bool accepted)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}
		if (ghost.GetModifier<ValidAdorcismGhostModifier>(x => x.Vessel.PlayerId == vessel.PlayerId) is not { } mod)
		{
			Error($"RpcPossess - Invalid poltergeist");
			return;
		}
		if (vessel == null || vessel.Data == null || vessel.HasDied())
		{
			return;
		}
		if (vessel.Data.Role is not VesselRole)
		{
			Error("RpcPossess - Invalid Vessel target");
			return;
		}

		if (ghost.AmOwner)
		{
			mod.ClearNotifications();
		}

		if (accepted)
		{
			GhostPossession(ghost, vessel);
		}
		else
		{
			VesselRejection(ghost, vessel);
		}
	}

	private static void VesselRejection(PlayerControl ghost, PlayerControl vessel)
	{
		VesselControlState.SetTimerActive(vessel.PlayerId);

		if (!ghost.AmOwner && !vessel.AmOwner)
		{
			return;
		}

		var text = vessel.AmOwner ?
			TouLocale.GetParsed("VesselRoleYouDeniedPossession").Replace("<player>", ghost.Data.PlayerName) :
			TouLocale.GetParsed("VesselRoleVesselHasDenied").Replace("<player>", vessel.Data.PlayerName);

		var notif1 = Helpers.CreateAndShowNotification(text, Color.white, new Vector3(0f, 1f, -20f),
				spr: VesselRoleIcons.Vessel.LoadAsset());

		notif1.AdjustNotification();
	}

	private static void GhostPossession(PlayerControl ghost, PlayerControl vessel)
	{
		var mod = new PoltergeistModifier(vessel);
		ghost.AddModifier(mod);

		VesselControlState.SetControl(vessel.PlayerId, ghost.PlayerId);
		if (!vessel.HasModifier<VesselPossessedModifier>())
		{
			vessel.AddModifier<VesselPossessedModifier>(ghost);
		}
		if (vessel.HasModifier<VesselAdorcismModifier>())
		{
			vessel.RemoveModifier<VesselAdorcismModifier>();
		}

		foreach (var validmod in ModifierUtils.GetActiveModifiers<ValidAdorcismGhostModifier>(m => m.Vessel.PlayerId == vessel.PlayerId))
		{
			VesselClosed(validmod.Player, vessel);
		}
		foreach (var validmod2 in ghost.GetModifiers<ValidAdorcismGhostModifier>())
		{
			VesselClosed(ghost, validmod2.Vessel);
		}

		if (vessel.inVent)
		{
			vessel.MyPhysics.ExitAllVents();
		}

		var pos = (Vector2)vessel.transform.position;
		if (vessel.AmOwner)
		{
			if (vessel.NetTransform != null)
			{
				try
				{
					vessel.NetTransform.SnapTo(pos);
				}
				catch
				{
					// ignored
				}
			}
		}
		else if (ghost.AmOwner)
		{
			NetTransformBacklogUtils.FlushAndSnap(vessel);
		}
		else
		{
			NetTransformBacklogUtils.FlushBacklog(vessel);
		}

		if (ghost.NetTransform != null)
		{
			try
			{
				ghost.NetTransform.SnapTo(pos);
			}
			catch
			{
				// ignored
			}
		}

		if (ghost.AmOwner)
		{
			CustomButtonSingleton<PoltergeistKillButton>.Instance.SetActive(true, ghost.Data.Role);
			CustomButtonSingleton<PoltergeistVentButton>.Instance.SetActive(true, ghost.Data.Role);
			mod.CreateNotification();
		}
		else if (vessel.AmOwner)
		{
			CustomButtonSingleton<VesselAdorciseButton>.Instance.ActivateTriggerEffect();
		}

		if (!VesselControlState.CanShareControl && (ghost.AmOwner || vessel.AmOwner))
		{
			CustomButtonSingleton<VesselChangeControlButton>.Instance.SetActive(true, PlayerControl.LocalPlayer.Data.Role);
		}
	}

	[MethodRpc((uint)VesselModRpc.VesselEndPossession)]
	public static void RpcGhostEndPossession(PlayerControl ghost, PlayerControl vessel)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}

		GhostEndPossession(ghost, vessel);
	}

	public static void GhostEndPossession(PlayerControl ghost, PlayerControl vessel)
	{
		if (ghost.GetModifier<PoltergeistModifier>(x => x.Vessel.PlayerId == vessel.PlayerId) is not { } mod)
		{
			return;
		}

		if (vessel != null && vessel.GetModifier<VesselPossessedModifier>() is { } mod1)
		{
			VesselControlState.ClearControl(vessel.PlayerId);
			if (vessel.TryGetModifier<VesselPossessedModifier>(out var mod2))
			{
				vessel.RemoveModifier(mod2);
			}

			if (vessel.MyPhysics != null)
			{
				if (vessel.MyPhysics.body != null)
				{
					vessel.MyPhysics.body.velocity = Vector2.zero;
				}
				vessel.MyPhysics.SetNormalizedVelocity(Vector2.zero);
			}

			var finalPos = (Vector2)vessel.transform.position;
			if (vessel.NetTransform != null)
			{
				try
				{
					NetTransformBacklogUtils.FlushBacklog(vessel);

					if (vessel.AmOwner)
					{
						vessel.NetTransform.SnapTo(finalPos);
					}
					else if (ghost != null && ghost.AmOwner)
					{
						NetTransformBacklogUtils.FlushAndSnap(vessel);
					}
					else
					{
						NetTransformBacklogUtils.FlushAndSnap(vessel);
					}
				}
				catch
				{
					// ignored
				}
			}
		}

		if (ghost != null)
		{
			ghost.RemoveModifier(mod);

			if (ghost.AmOwner)
			{
				var pos = (Vector2)ghost.transform.position;
				if (ghost.NetTransform != null)
				{
					try
					{
						ghost.NetTransform.SnapTo(pos);
					}
					catch
					{
						// ignored
					}
				}
			}
			else
			{
				NetTransformBacklogUtils.FlushBacklog(ghost);
			}

			if (ghost.AmOwner)
			{
				CustomButtonSingleton<PoltergeistKillButton>.Instance.SetActive(false, ghost.Data.Role);
				CustomButtonSingleton<PoltergeistVentButton>.Instance.SetActive(false, ghost.Data.Role);
				CustomButtonSingleton<PoltergeistPossessButton>.Instance.SetActive(false, ghost.Data.Role);
				ControlledPlayerInteractionPatches.ClearInteractableOutlines();
				ControlledPlayerInteractionPatches.ClearVentOutlines();
			}
		}

		mod.ClearNotification();

		if (!VesselControlState.CanShareControl && (ghost != null && ghost.AmOwner || vessel != null && vessel.AmOwner))
		{
			CustomButtonSingleton<VesselChangeControlButton>.Instance.SetActive(false, PlayerControl.LocalPlayer.Data.Role);
		}
	}

	[MethodRpc((uint)VesselModRpc.ChangePossessionControl)]
	public static void RpcChangeControl(PlayerControl ghost, PlayerControl vessel)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}
		if (ghost.GetModifier<PoltergeistModifier>(x => x.Vessel.PlayerId == vessel.PlayerId) is not { } mod)
		{
			Error($"RpcChangeControl - Invalid poltergeist");
			return;
		}
		if (vessel == null || vessel.Data == null || vessel.HasDied())
		{
			return;
		}

		VesselControlState.SwapControlOver(ghost.PlayerId, vessel.PlayerId);

		if (ghost.AmOwner || vessel.AmOwner)
		{
			if (vessel.inVent)
			{
				Vent.currentVent.SetButtons(true);
			}
			if (!VesselControlState.HasControl(PlayerControl.LocalPlayer.PlayerId))
			{
				ControlledPlayerInteractionPatches.ClearVentOutlines();
			}
		}
	}

	[MethodRpc((uint)VesselModRpc.VesselTriggerInteraction)]
	public static void RpcGhostTriggerInteraction(PlayerControl ghost, PlayerControl vessel, Vector2 interactablePosition)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}
		if (ghost.GetModifier<PoltergeistModifier>(x => x.Vessel.PlayerId == vessel.PlayerId) is not { } mod)
		{
			Error($"RpcVesselInteraction - Invalid poltergeist");
			return;
		}
		if (vessel == null || vessel.Data == null || vessel.HasDied())
		{
			return;
		}

		if (!VesselControlState.IsControllingActionable(ghost.PlayerId))
		{
			return;
		}

		var (interactable, _) = ControlledPlayerInteractionPatches.FindClosestInteractable(vessel, interactablePosition);
		if (interactable == null)
		{
			return;
		}

		TriggerInteractionAsPlayer(vessel, interactable);
	}

	private static void TriggerInteractionAsPlayer(PlayerControl player, IUsable interactable)
	{
		if (player == null || interactable == null)
		{
			return;
		}

		if (interactable.TryCast<Ladder>() is { } ladder)
		{
			if (!player.AmOwner || ladder.IsCoolingDown())
			{
				return;
			}
			player.MyPhysics.RpcClimbLadder(ladder);
			ladder.CoolDown = ladder.MaxCoolDown;
		}
		else if (interactable.TryCast<ZiplineConsole>() is { } ziplineConsole)
		{
			if (!player.AmOwner || ziplineConsole.IsCoolingDown())
			{
				return;
			}
			ziplineConsole.zipline.Use(ziplineConsole.atTop, ziplineConsole);
			ziplineConsole.CoolDown = ziplineConsole.MaxCoolDown;
		}
		else if (interactable.TryCast<OpenDoorConsole>() is { } openDoorConsole)
		{
			if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
			{
				return;
			}
			openDoorConsole.myDoor.SetDoorway(true);
		}
		else if (interactable.TryCast<DoorConsole>() is { } doorConsole)
		{
			if (player.AmOwner)
			{
				player.NetTransform.Halt();
				var minigame = Object.Instantiate(doorConsole.MinigamePrefab, Camera.main.transform);
				minigame.transform.localPosition = new Vector3(0f, 0f, -50f);

				try
				{
					minigame.Cast<IDoorMinigame>().SetDoor(doorConsole.MyDoor);
				}
				catch (InvalidCastException)
				{
					/* ignored */
				}

				minigame.Begin(null);
			}
		}
		else if (interactable.TryCast<PlatformConsole>() is { } platformConsole)
		{
			if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
			{
				return;
			}
			var platform = platformConsole.Platform;
			if (platform != null)
			{
				var vector = platform.transform.position - player.transform.position;
				if (!platform.Target && vector.magnitude <= 3f)
				{
					platform.IsDirty = true;
					platform.StartCoroutine(platform.UsePlatform(player));
				}
			}
		}
		else if (interactable.TryCast<DeconControl>() is { } deconControl)
		{
			if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
			{
				return;
			}
			deconControl.cooldown = 6f;
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(deconControl.UseSound, false);
			}
			deconControl.OnUse.Invoke();
		}
	}

	[MethodRpc((uint)VesselModRpc.VesselMoveVent)]
	public static void RpcVesselTryMoveToVent(
		PlayerControl source,
		PlayerControl ghost, PlayerControl vessel, 
		int ventId, int otherVentId)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(source);
			return;
		}
		if (ghost.GetModifier<PoltergeistModifier>(x => x.Vessel.PlayerId == vessel.PlayerId) is not { } mod)
		{
			Error($"RpcVesselVentMove - Invalid poltergeist");
			return;
		}
		if (mod.Vessel.PlayerId != vessel.PlayerId)
		{
			Error("RpcVesselVentMove - Vessel is not controlled by ghost.");
		}
		if (vessel == null || vessel.Data == null || vessel.HasDied())
		{
			return;
		}

		Vent vent = ShipStatus.Instance.AllVents.First((Vent v) => v.Id == ventId);
		Vent otherVent = ShipStatus.Instance.AllVents.First((Vent v) => v.Id == otherVentId);

		Vector3 position = otherVent.transform.position;
		position -= (Vector3)vessel.Collider.offset;
		vessel.NetTransform.SnapTo(position);

		if (!ghost.AmOwner && !vessel.AmOwner)
		{
			return;
		}

		if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlaySound(ShipStatus.Instance.VentMoveSounds.ToArray().Random(), loop: false).pitch = FloatRange.Next(0.8f, 1.2f);
		}
		vent.SetButtons(enabled: false);
		otherVent.SetButtons(enabled: true);
		Vent.currentVent = otherVent;
		VentilationSystem.Update(VentilationSystem.Operation.Move, Vent.currentVent.Id);
	}

	public void LobbyStart()
	{
		VesselControlState.ClearAll();

		foreach (var ghostMod in ModifierUtils.GetActiveModifiers<PoltergeistModifier>())
		{
			ghostMod.Player.RemoveModifier(ghostMod);
		}
	}
}
