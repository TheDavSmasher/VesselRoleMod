using HarmonyLib;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using TownOfUs.Events.Crewmate;
using TownOfUs.Events.Modifiers;
using TownOfUs.Modifiers.Game.Crewmate;
using TownOfUs.Modules;
using TownOfUs.Modules.ControlSystem;
using TownOfUs.Options.Roles.Crewmate;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Utilities;


namespace VesselRoleMod.Patches.ControlSystem;

[HarmonyPatch]
public static class RoleSpecificPatches
{
	[HarmonyPatch(typeof(PuppeteerRole), nameof(PuppeteerRole.RpcPuppeteerControl))]
	[HarmonyPatch(typeof(ParasiteRole), nameof(ParasiteRole.RpcParasiteControl))]
	[HarmonyPostfix]
	public static void ControlledVesselPostfix(PlayerControl target)
	{
		if (target == null || target.Data == null || target.HasDied())
		{
			return;
		}

		if (ParasiteControlState.IsControlled(target.PlayerId, out _) ||
			PuppeteerControlState.IsControlled(target.PlayerId, out _))
		{
			target.RemoveExistingModifier<VesselAdorcismModifier>();
		}
	}

	[HarmonyPatch(typeof(CustomMurderRpc), nameof(CustomMurderRpc.RpcCustomMurder),
		[typeof(PlayerControl), typeof(PlayerControl), typeof(MeetingCheck),
		 typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool)])]
	[HarmonyPrefix]
	public static void TryMurderVesselGhostPrefix(PlayerControl source, ref PlayerControl target)
	{
		if (source.Data.Role is not VeteranRole)
		{
			return;
		}

		if (!target.Data.IsDead || target.Data.Disconnected)
		{
			return;
		}

		if (!target.TryGetModifier<PoltergeistModifier>(out var mod))
		{
			return;
		}

		target = mod.Vessel;
	}

	[HarmonyPatch(typeof(LookoutEvents), nameof(LookoutEvents.CheckForLookoutWatched))]
	[HarmonyPatch(typeof(PlaguebearerRole), nameof(PlaguebearerRole.CheckInfected))]
	[HarmonyPatch(typeof(HunterRole), nameof(HunterRole.RpcCatchPlayer))]
	[HarmonyPrefix]
	public static void GhostKillPlayerTrackedByRolePrefix(ref PlayerControl source)
	{
		if (!source.Data.IsDead || source.Data.Disconnected)
		{
			return;
		}

		if (!source.TryGetModifier<PoltergeistModifier>(out var mod))
		{
			return;
		}

		source = mod.Vessel;
	}

	[HarmonyPatch(typeof(DeputyEvents), nameof(DeputyEvents.AfterMurderEventHandler))]
	[HarmonyPatch(typeof(FrostyEvents), nameof(FrostyEvents.AfterMurderEventHandler))]
	[HarmonyPatch(typeof(BaitEvents), nameof(BaitEvents.AfterMurderEventHandler))]
	[HarmonyPrefix]
	public static void GhostKillPlayerTrackedByEventPrefix(ref AfterMurderEvent @event)
	{
		var source = @event.Source;
		if (!source.Data.IsDead || source.Data.Disconnected)
		{
			return;
		}

		if (!source.TryGetModifier<PoltergeistModifier>(out var mod))
		{
			return;
		}

		source = mod.Vessel;

		@event = new(
			source,
			@event.Target,
			@event.DeadBody
		);
	}

	[HarmonyPatch(typeof(CelebrityModifier), nameof(CelebrityModifier.CelebrityKilled))]
	[HarmonyPrefix]
	public static void GhostKillPlayerTrackingKillerPrefix(ref PlayerControl source) // After Murder
	{
		if (!source.Data.IsDead || source.Data.Disconnected)
		{
			return;
		}

		if (OptionGroupSingleton<VesselOptions>.Instance.ReportGhostInstead)
		{
			return;
		}

		if (!source.TryGetModifier<PoltergeistModifier>(out var mod))
		{
			return;
		}

		source = mod.Vessel;
	}

	[HarmonyPatch(typeof(TelepathEvents), nameof(TelepathEvents.AfterMurderEventHandler))]
	[HarmonyPrefix]
	public static bool GhostImpostorKillPrefix(ref AfterMurderEvent @event)
	{
		var source = @event.Source;
		if (!source.Data.IsDead || source.Data.Disconnected)
		{
			return true;
		}

		if (!source.TryGetModifier<PoltergeistModifier>(out var mod))
		{
			return true;
		}

		return true;
	}

	[HarmonyPatch(typeof(MirrorcasterRole), nameof(MirrorcasterRole.RpcMagicMirrorAttacked))]
	[HarmonyPrefix]
	public static bool GhostAttackMagicMirrorPrefix(ref PlayerControl source, PlayerControl mirrorcaster)
	{
		if (LobbyBehaviour.Instance)
		{
			return true;
		}

		if (mirrorcaster.Data.Role is not MirrorcasterRole role)
		{
			return true;
		}

		if (!source.Data.IsDead || source.Data.Disconnected)
		{
			return true;
		}

		if (OptionGroupSingleton<VesselOptions>.Instance.ReportGhostInstead)
		{
			return true;
		}

		if (!source.TryGetModifier<PoltergeistModifier>(out var mod))
		{
			return true;
		}

		source = mod.Vessel;

		if (mod.Vessel.AmOwner)
		{
			role.SetProtectedPlayer(null);
			role.UnleashesAvailable++;

			role.ContainedRole = source.GetRoleWhenAlive();

			return false;
		}

		var opt = OptionGroupSingleton<MirrorcasterOptions>.Instance;
		if (opt.WhoGetsNotification is MirrorOption.MirrorcasterAndKiller && mod.Ghost.AmOwner)
		{
			MirrorcasterRole.DangerAnim();
		}

		return true;
	}
}
