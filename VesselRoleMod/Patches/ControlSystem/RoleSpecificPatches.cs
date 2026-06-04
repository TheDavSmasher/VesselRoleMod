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
using TownOfUs.Roles;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
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
		if (!target.Data.IsDead || target.Data.Disconnected)
		{
			return;
		}

		if (!target.TryGetModifier<PoltergeistModifier>(out var mod))
		{
			return;
		}

		if (source.Data.Role is not VeteranRole)
		{
			return;
		}

		target = mod.Target;
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

		source = mod.Target;
	}

	//[HarmonyPatch(typeof(CelebrityEvents), nameof(CelebrityEvents.AfterMurderEventHandler))]
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

		source = mod.Target;

		@event = new(
			source,
			@event.Target,
			@event.DeadBody
		);
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
	public static class GhostAttackMagicMirrorPatch
	{
		public static bool Prefix(ref PlayerControl source, ref bool __state)
		{
			if (LobbyBehaviour.Instance)
			{
				return true;
			}

			if (!source.Data.IsDead || source.Data.Disconnected)
			{
				return true;
			}

			if (!source.TryGetModifier<PoltergeistModifier>(out var mod))
			{
				return true;
			}

			if (mod.Player.AmOwner || mod.Target.AmOwner)
			{
				__state = true;
				return false;
			}

			source = mod.Target;

			return true;
		}

		public static void Postfix(PlayerControl source, PlayerControl mirrorcaster, bool __state)
		{
			if (!__state)
			{
				return;
			}

			var mod = source.GetModifier<PoltergeistModifier>()!;
			source = mod.Target;

			// Execute logic previously skipped
			if (mirrorcaster.Data.Role is not MirrorcasterRole role)
			{
				Error("RpcMagicMirrorAttacked - Invalid mirrorcaster");
				return;
			}

			role.SetProtectedPlayer(null);
			role.UnleashesAvailable++;

			var killerRole = source.GetRoleWhenAlive();
			if (killerRole is MirrorcasterRole mirrorcaster2)
			{
				role.ContainedRole = mirrorcaster2.ContainedRole;
				mirrorcaster2.ContainedRole = null;
			}

			if (source.Data.Role is IGhostRole)
			{
				killerRole = source.Data.Role;
			}

			role.ContainedRole = killerRole;

			var opt = OptionGroupSingleton<MirrorcasterOptions>.Instance;
			if (opt.WhoGetsNotification is MirrorOption.MirrorcasterAndKiller && mod.Player.AmOwner)
			{
				MirrorcasterRole.DangerAnim();
			}
		}
	}
}
