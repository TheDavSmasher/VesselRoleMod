using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities.Extensions;
using System;
using System.Collections.Generic;
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
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.Components;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Patches.ControlSystem.Interactions;
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
		targetPlayer.RemoveExistingModifier<VesselBlacklistModifier>();
	}

	public override void OnMeetingStart()
	{
		RoleBehaviourStubs.OnMeetingStart(this);
	}

	public static PlayerControl GetReportedKiller(PlayerControl vessel, PlayerControl ghost)
	{
		return ghost;
	}

	#region Role RPCs

	#region Vessel Seeking
	public static void RpcSeekVessel(PlayerControl vessel, List<PlayerControl> ghosts)
	{
		RpcSeekVessel(vessel, ghosts.ToDictionary(x => x.PlayerId, x => x.Data.PlayerName));
	}

	[MethodRpc((uint)VesselModRpc.AdorcismStart)]
	public static void RpcSeekVessel(PlayerControl vessel, Dictionary<byte, string> ghosts)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(vessel);
			return;
		}
		if (vessel.Data.Role is not VesselRole)
		{
			Error($"RpcSeekVessel - Invalid Vessel target");
			return;
		}

		if (!vessel.HasModifier<VesselAdorcismModifier>())
		{
			vessel.AddModifier<VesselAdorcismModifier>();
		}

		if (ghosts.Count == 0)
		{
			return;
		}

		var color = Palette.PlayerColors[vessel.GetDefaultAppearance().ColorId];
		var allPlayers = PlayerControl.AllPlayerControls.ToArray().Where(x => x.PlayerId != vessel.PlayerId).ToList();

		foreach (var (ghostId, ghostName) in ghosts)
		{
			var ghost = allPlayers.FirstOrDefault(x => x.PlayerId == ghostId || x.Data.PlayerName == ghostName);
			if (ghost == null)
			{
				continue;
			}
			allPlayers.Remove(ghost);

			if (!ghost.HasModifier<ValidAdorcismGhostModifier>(x => x.Vessel.PlayerId == vessel.PlayerId))
			{
				ghost.AddModifier<ValidAdorcismGhostModifier>(vessel);
			}

			if (ghost.AmOwner)
			{
				var mod = new PoltergeistArrowModifier(vessel, color);
				ghost.AddModifier(mod);

				CustomButtonSingleton<PoltergeistPossessButton>.Instance.SetActive(true, ghost.Data.Role);
			}
		}
	}

	[MethodRpc((uint)VesselModRpc.AdorcismEnd)]
	public static void RpcVesselClosed(PlayerControl vessel)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(vessel);
			return;
		}
		if (vessel.Data.Role is not VesselRole)
		{
			Error($"RpcSeekVessel - Invalid Vessel target");
			return;
		}

		vessel.RemoveExistingModifier<VesselAdorcismModifier>();
	}

	public static void VesselClosed(PlayerControl vessel, PlayerControl? ghost = null)
	{
		foreach (var validMod in ModifierUtils.GetActiveModifiers
			<ValidAdorcismGhostModifier>(x => IsModifierToRemove(x, vessel, ghost)))
		{
			if (validMod == null)
			{
				continue;
			}
			validMod.RemoveSelf();

			var seekingGhost = validMod.Player;

			if (seekingGhost.AmOwner)
			{
				seekingGhost.RemoveExistingModifier<PoltergeistArrowModifier>(x => IsModifierToRemove(x, vessel, ghost));

				if (!seekingGhost.HasModifierOfType<IVesselSeekingModifier>())
				{
					CustomButtonSingleton<PoltergeistPossessButton>.Instance.SetActive(false, seekingGhost.Data.Role);
				}
			}
		}
	}

	private static bool IsModifierToRemove<T>(T modifier, PlayerControl vessel, PlayerControl? ghost) where T : BaseModifier, IVesselModifier
	{
		return vessel.PlayerId == modifier.Vessel.PlayerId ||
			   ghost?.PlayerId == modifier.Player.PlayerId;
	}
	#endregion

	#region Ghost Possession

	#region Start Possession
	[MethodRpc((uint)VesselModRpc.VesselTryPossessing)]
	public static void RpcGhostTryPossessing(PlayerControl ghost, PlayerControl vessel)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}
		if (!ghost.TryGetModifier<ValidAdorcismGhostModifier>(out var mod, x => x.Vessel.PlayerId == vessel.PlayerId))
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
			MiscUtils.RunAnticheatWarning(vessel);
			return;
		}
		if (!ghost.TryGetModifier<ValidAdorcismGhostModifier>(out var mod, x => x.Vessel.PlayerId == vessel.PlayerId))
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
		vessel.RemoveExistingModifier<VesselAdorcismModifier>();

		VesselClosed(vessel, ghost);

		vessel.MyPhysics.ClearVentState();

		var pos = (Vector2)vessel.transform.position;
		if (vessel.AmOwner)
		{
			vessel.NetTransform?.SnapTo(pos);
		}
		else if (ghost.AmOwner)
		{
			NetTransformBacklogUtils.FlushAndSnap(vessel);
		}
		else
		{
			NetTransformBacklogUtils.FlushBacklog(vessel);
		}

		ghost.NetTransform?.SnapTo(pos);

		var ventButton = CustomButtonSingleton<PoltergeistVentButton>.Instance;
		if (ghost.AmOwner)
		{
			CustomButtonSingleton<PoltergeistKillButton>.Instance.SetActive(true, ghost.Data.Role);
			ventButton.SetActive(true, ghost.Data.Role);
			mod.CreateNotification();
		}
		else if (vessel.AmOwner)
		{
			CustomButtonSingleton<VesselAdorciseButton>.Instance.ActivateTriggerEffect();
		}

		if (ghost.AmOwner || vessel.AmOwner)
		{
			ventButton.SetTimer(ventButton.InitialCooldown);
			if (!VesselControlState.CanShareControl)
			{
				CustomButtonSingleton<VesselChangeControlButton>.Instance.SetActive(true, PlayerControl.LocalPlayer.Data.Role);
			}
		}
	}
	#endregion

	#region End Possession
	[MethodRpc((uint)VesselModRpc.VesselEndPossession)]
	public static void RpcGhostEndPossession(PlayerControl ghost, PlayerControl vessel, bool onKill = false)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}

		GhostEndPossession(ghost, vessel, onKill);
	}

	public static void GhostEndPossession(PlayerControl ghost, PlayerControl vessel, bool onKill = false)
	{
		if (vessel == null || ghost == null)
		{
			return;
		}

		if (!ghost.TryGetModifier<PoltergeistModifier>(out var mod, x => x.Vessel.PlayerId == vessel.PlayerId))
		{
			return;
		}

		if (vessel.TryGetModifier<VesselPossessedModifier>(out var mod1))
		{
			VesselControlState.ClearControl(vessel.PlayerId);
			mod1.RemoveSelf();

			if (vessel.MyPhysics != null)
			{
				if (vessel.inVent)
				{
					vessel.MyPhysics.ClearVentState();

					if (ghost.AmOwner &&
						CustomButtonSingleton<PoltergeistVentButton>.Instance.HasEffect)
					{
						ghost.AddModifier<GhostEngineerCooldownModifier>();
					}
				}
				else
				{
					vessel.MyPhysics.body?.velocity = Vector2.zero;
					vessel.MyPhysics.SetNormalizedVelocity(Vector2.zero);
				}
			}

			var finalPos = (Vector2)vessel.transform.position;

			NetTransformBacklogUtils.FlushBacklog(vessel);

			if (vessel.AmOwner)
			{
				vessel.NetTransform?.SnapTo(finalPos);
			}
			else if (ghost.AmOwner)
			{
				NetTransformBacklogUtils.FlushAndSnap(vessel);
			}
			else
			{
				NetTransformBacklogUtils.FlushAndSnap(vessel);
			}
		}

		mod.RemoveSelf();

		if (ghost.AmOwner)
		{
			var pos = (Vector2)ghost.transform.position;
			ghost.NetTransform?.SnapTo(pos);
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
		}

		mod.ClearNotification();

		if (!VesselControlState.CanShareControl && (ghost.AmOwner || vessel.AmOwner))
		{
			CustomButtonSingleton<VesselChangeControlButton>.Instance.SetActive(false, PlayerControl.LocalPlayer.Data.Role);
		}

		if (onKill)
		{
			ghost.AddModifier<GhostKillerBlockModifier>(vessel.AmOwner);
		}
	}
	#endregion

	#region Possession Control
	[MethodRpc((uint)VesselModRpc.ChangePossessionControl)]
	public static void RpcChangeControl(PlayerControl ghost, PlayerControl vessel)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}
		if (!ghost.TryGetModifier<PoltergeistModifier>(out var mod, x => x.Vessel.PlayerId == vessel.PlayerId))
		{
			Error($"RpcChangeControl - Invalid poltergeist");
			return;
		}
		if (vessel == null || vessel.Data == null || vessel.HasDied())
		{
			return;
		}

		VesselControlState.SwapControlOver(ghost.PlayerId, vessel.PlayerId);

		if ((ghost.AmOwner || vessel.AmOwner) && vessel.inVent)
		{
			Vent.currentVent.SetButtons(true);
		}
	}

	#region Possession Interactions

	#region Usables
	[MethodRpc((uint)VesselModRpc.VesselTriggerInteraction)]
	public static void RpcGhostTriggerInteraction(PlayerControl ghost, PlayerControl vessel, Vector2 interactablePosition)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}
		if (!ghost.TryGetModifier<PoltergeistModifier>(out var mod, x => x.Vessel.PlayerId == vessel.PlayerId))
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

		var interactable = ControlledPlayerInteractionPatches.FindClosestInteractable(vessel, interactablePosition);
		if (interactable == null)
		{
			return;
		}

		TriggerInteractionAsPlayer(vessel, interactable);
		SetStateForGhost(ghost, interactable);
	}

	[MethodRpc((uint)VesselModRpc.VesselSetGhostState)]
	public static void RpcVesselSetGhostState(PlayerControl ghost, PlayerControl vessel, Vector2 interactablePosition)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(vessel);
			return;
		}
		if (!ghost.AmOwner)
		{
			return;
		}

		if (!ghost.TryGetModifier<PoltergeistModifier>(out var mod, x => x.Vessel.PlayerId == vessel.PlayerId))
		{
			Error($"RpcVesselSetState - Invalid poltergeist");
			return;
		}
		if (vessel == null || vessel.Data == null || vessel.HasDied())
		{
			return;
		}

		if (VesselControlState.IsFullyControlling(ghost.PlayerId))
		{
			return;
		}

		var interactable = ControlledPlayerInteractionPatches.FindClosestInteractable(vessel, interactablePosition);
		if (interactable == null)
		{
			return;
		}

		SetStateForGhost(ghost, interactable);
	}

	public static void SetStateForGhost(PlayerControl ghost, IUsable interactable)
	{
		if (ghost == null || interactable == null)
		{
			return;
		}

		if (!ghost.AmOwner)
		{
			return;
		}

		if (interactable.TryCast<Ladder>() is { } ladder)
		{
			if (ladder.IsCoolingDown())
			{
				return;
			}
			ladder.CoolDown = ladder.MaxCoolDown;
		}
		else if (interactable.TryCast<ZiplineConsole>() is { } zipline)
		{
			if (zipline.IsCoolingDown())
			{
				return;
			}
			zipline.CoolDown = zipline.MaxCoolDown;
			zipline.zipline.lastUsedConsole = zipline;
		}
		else if (interactable.TryCast<DeconControl>() is { } decon)
		{
			decon.cooldown = 6f;
		}
	}

	private static void TriggerInteractionAsPlayer(PlayerControl player, IUsable interactable)
	{
		if (player == null || interactable == null)
		{
			return;
		}

		if (!player.AmOwner)
		{
			return;
		}

		if (interactable.TryCast<Ladder>() is { } ladder)
		{
			if (ladder.IsCoolingDown())
			{
				return;
			}
			player.MyPhysics.RpcClimbLadder(ladder);
			ladder.CoolDown = ladder.MaxCoolDown;
		}
		else if (interactable.TryCast<ZiplineConsole>() is { } ziplineConsole)
		{
			if (ziplineConsole.IsCoolingDown())
			{
				return;
			}
			ziplineConsole.zipline.Use(ziplineConsole.atTop, ziplineConsole);
			ziplineConsole.CoolDown = ziplineConsole.MaxCoolDown;
		}
		else if (interactable.TryCast<OpenDoorConsole>() is { } openDoorConsole)
		{
			ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(openDoorConsole.myDoor.Id | 0x40));
			openDoorConsole.myDoor.SetDoorway(true);
		}
		else if (interactable.TryCast<DoorConsole>() is { } doorConsole)
		{
			player.NetTransform.Halt();
			var minigame = Object.Instantiate(doorConsole.MinigamePrefab, Camera.main.transform);
			minigame.transform.localPosition = new Vector3(0f, 0f, -50f);
			minigame.TryCast<IDoorMinigame>()?.SetDoor(doorConsole.MyDoor);
			minigame.Begin(null);
		}
		else if (interactable.TryCast<PlatformConsole>() is { } platformConsole)
		{
			platformConsole.Platform.Use();
		}
		else if (interactable.TryCast<DeconControl>() is { } deconControl)
		{
			deconControl.cooldown = 6f;
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(deconControl.UseSound, false);
			}
			deconControl.OnUse.Invoke();
		}
	}
	#endregion

	#region Vents
	[MethodRpc((uint)VesselModRpc.VesselEnterVent)]
	public static void RpcVesselEnterVent(PlayerControl ghost, PlayerControl vessel, int id)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}
		if (!ghost.TryGetModifier<PoltergeistModifier>(out var mod, x => x.Vessel.PlayerId == vessel.PlayerId))
		{
			Error($"RpcVesselVentEnter - Invalid poltergeist");
			return;
		}
		if (mod.Vessel.PlayerId != vessel.PlayerId)
		{
			Error("RpcVesselVentEnter - Vessel is not controlled by ghost.");
		}
		if (vessel == null || vessel.Data == null || vessel.HasDied())
		{
			return;
		}

		if (!vessel.AmOwner)
		{
			return;
		}

		vessel.MyPhysics.RpcEnterVent(id);

		var button = CustomButtonSingleton<PoltergeistVentButton>.Instance;
		if (button.HasEffect)
		{
			button.EffectActive = true;
			button.Timer = button.EffectDuration;
		}
		else
		{
			button.Timer = button.Cooldown;
		}
	}

	[MethodRpc((uint)VesselModRpc.VesselExitVent)]
	public static void RpcVesselExitVent(
		PlayerControl source,
		PlayerControl ghost, PlayerControl vessel,
		int id
		)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(source);
			return;
		}
		if (!ghost.TryGetModifier<PoltergeistModifier>(out var mod, x => x.Vessel.PlayerId == vessel.PlayerId))
		{
			Error($"RpcVesselVentExit - Invalid poltergeist");
			return;
		}
		if (mod.Vessel.PlayerId != vessel.PlayerId)
		{
			Error("RpcVesselVentExit - Vessel is not controlled by ghost.");
		}
		if (vessel == null || vessel.Data == null || vessel.HasDied())
		{
			return;
		}

		if (source.AmOwner)
		{
			vessel.MyPhysics.RpcExitVent(id);
		}
		else if (ghost.AmOwner || vessel.AmOwner)
		{
			var button = CustomButtonSingleton<PoltergeistVentButton>.Instance;
			if (!button.HasEffect || button.EffectActive)
			{
				button.EffectActive = false;
			}
			button.Timer = button.Cooldown;

			if (ghost.AmOwner && button.HasEffect)
			{
				ghost.AddModifier<GhostEngineerCooldownModifier>();
			}
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
		if (!ghost.TryGetModifier<PoltergeistModifier>(out var mod, x => x.Vessel.PlayerId == vessel.PlayerId))
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

		Vent vent = ShipStatus.Instance.AllVents.First(v => v.Id == ventId);
		Vent otherVent = ShipStatus.Instance.AllVents.First(v => v.Id == otherVentId);

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
	#endregion

	#endregion

	#endregion

	#endregion

	#endregion

	public void LobbyStart()
	{
		VesselControlState.ClearAll();

		foreach (var ghostMod in ModifierUtils.GetActiveModifiers<PoltergeistModifier>())
		{
			ghostMod.RemoveSelf();
		}
	}
}
