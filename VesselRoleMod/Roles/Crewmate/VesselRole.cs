using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using System;
using System.Collections.Generic;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Game.Universal;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Buttons.Modifiers;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
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

	public PlayerControl? Ghost { get; set; }

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
		if (player.HasModifier<ValidAdorcismGhostModifier>(x => x.Vessel.PlayerId == target.PlayerId))
		{
			Error("RpcSeekVessel - Invalid ghost");
			return;
		}
		if (target.Data.Role is not VesselRole)
		{
			Error("RpcSeekVessel - Invalid Vessel target");
			return;
		}

		player.AddModifier<ValidAdorcismGhostModifier>(target);

		var color = Palette.PlayerColors[target.GetDefaultAppearance().ColorId];
		if (target.AmOwner)
		{
			var mod = new PoltergeistArrowModifier(target, color);
			player.AddModifier(mod);
		}
	}

	[MethodRpc((uint)VesselModRpc.AdorcismEnd)]
	public static void RpcVesselClosed(PlayerControl player, PlayerControl target)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(player);
			return;
		}
		if (target.Data.Role is not VesselRole)
		{
			Error("RpcVesselClosed - Invalid Vessel target");
			return;
		}

		if (player.TryGetModifier<ValidAdorcismGhostModifier>(out var mod, x => x.Vessel.PlayerId == target.PlayerId))
		{
			player.RemoveModifier(mod);
		}
		else
		{
			Error("RpcVesselClosed - Invalid ghost");
		}
	}

	[MethodRpc((uint)VesselModRpc.VesselPossession)]
	public static void RpcGhostPossession(PlayerControl ghost, PlayerControl vessel)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}
		if (!ghost.HasModifier<ValidAdorcismGhostModifier>(x => x.Vessel.PlayerId == vessel.PlayerId))
		{
			Error("RpcPossess - Invalid poltergeist");
			return;
		}
		if (vessel == null || vessel.Data == null || vessel.Data.Role is not VesselRole role || vessel.HasDied())
		{
			Error("RpcPossess - Invalid Vessel target");
			return;
		}

		if (vessel.IsInTargetingAnimState())
		{
			return;
		}

		var mod = new PoltergeistModifier(vessel);
		ghost.AddModifier(mod);
		role.Ghost = ghost;

		VesselControlState.SetControl(vessel.PlayerId, ghost.PlayerId);
		if (vessel.HasModifier<VesselPossessedModifier>())
		{
			vessel.AddModifier<VesselPossessedModifier>(ghost);
		}

		if (vessel.inVent)
		{
			vessel.MyPhysics.ExitAllVents();
		}

		if (vessel.AmOwner)
		{
			var pos = (Vector2)vessel.transform.position;
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

		if (ghost.AmOwner)
		{
			CustomButtonSingleton<PoltergeistKillButton>.Instance.SetActive(true, ghost.Data.Role);
			mod.CreateNotification();

			ShyModifier.SetVisibility(ghost, 0.5f, true);
			// TODO: Make Ghost snap to vessel position at all times
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
		if (ghost.GetModifier<PoltergeistModifier>(x => x.Vessel.PlayerId == vessel.PlayerId) is not { } mod)
		{
			Error("RpcPossess - Invalid poltergeist");
			return;
		}

		if (vessel != null && vessel.Data.Role is VesselRole role)
		{
			VesselControlState.ClearControl(vessel.PlayerId);
			if (vessel.TryGetModifier<VesselPossessedModifier>(out var mod2))
			{
				vessel.RemoveModifier(mod2);
			}
			role.Ghost = null;

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
						NetTransformBacklogUtils.FlushBacklog(vessel);
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

				ShyModifier.SetVisibility(ghost, mod.GhostVisibility, true);
			}
		}

		mod.ClearNotifications();
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
			Error("RpcPossess - Invalid poltergeist");
			return;
		}
		if (vessel == null || vessel.Data == null || vessel.Data.Role is not VesselRole role || vessel.HasDied())
		{
			Error("RpcPossess - Invalid Vessel target");
			return;
		}

		if (mod.Vessel != vessel || role.Ghost != ghost || !VesselControlState.IsControlled(vessel.PlayerId, out _))
		{
			return;
		}

		var interactable = FindInteractableAtPosition(interactablePosition, vessel);
		if (interactable == null)
		{
			return;
		}

		TriggerInteractionAsPlayer(vessel, interactable);
	}

	private static IUsable? FindInteractableAtPosition(Vector2 position, PlayerControl player)
	{
		if (player == null)
		{
			return null;
		}

		var closestDistance = float.MaxValue;
		IUsable? closestInteractable = null;

		var cached = ControlledPlayerInteractionPatches.GetCachedInteractables();
		var interactablesToCheck = cached != null && cached.Count > 0
			? cached
			: GetInteractablesList();

		const float maxCheckDistance = 5f;

		foreach (var usable in interactablesToCheck)
		{
			if (usable == null)
			{
				continue;
			}

			var obj = usable.TryCast<MonoBehaviour>();
			if (obj == null)
			{
				continue;
			}

			var objPos = (Vector2)obj.transform.position;
			var distance = Vector2.Distance(position, objPos);
			if (distance > maxCheckDistance || distance > usable.UsableDistance)
			{
				continue;
			}

			bool canUse;
			usable.CanUse(player.Data, out canUse, out _);
			if (!canUse)
			{
				continue;
			}

			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestInteractable = usable;
			}
		}

		return closestInteractable;
	}

	private static List<IUsable> GetInteractablesList()
	{
		var result = new List<IUsable>();
		var allUsables = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
		foreach (var obj in allUsables)
		{
			if (obj.TryCast<IUsable>() is { } usable && usable.TryCast<Vent>() == null)
			{
				result.Add(usable);
			}
		}
		return result;
	}

	private static void TriggerInteractionAsPlayer(PlayerControl player, IUsable interactable)
	{
		if (player == null || interactable == null)
		{
			return;
		}

		if (interactable.TryCast<Ladder>() is { } ladder)
		{
			if (!player.AmOwner)
			{
				return;
			}
			player.MyPhysics.RpcClimbLadder(ladder);
			ladder.CoolDown = ladder.MaxCoolDown;
		}
		else if (interactable.TryCast<ZiplineConsole>() is { } ziplineConsole)
		{
			if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
			{
				return;
			}
			if (ziplineConsole.zipline != null)
			{
				player.CheckUseZipline(player, ziplineConsole.zipline, ziplineConsole.atTop);
			}
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

	public void LobbyStart()
	{
		VesselControlState.ClearAll();

		foreach (var ghostMod in ModifierUtils.GetActiveModifiers<PoltergeistModifier>())
		{
			ghostMod.Player.RemoveModifier(ghostMod);
		}
	}
}
