using HarmonyLib;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using Reactor.Utilities;
using System.Collections;
using System.Linq;
using TownOfUs.Buttons.Crewmate;
using TownOfUs.Events.Crewmate;
using TownOfUs.Events.Modifiers;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game;
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
using VesselRoleMod.Modules.ControlSystem;
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

		if (!source.TryGetModifier<PoltergeistModifier>(out var mod))
		{
			return;
		}

		source = mod.ReportedPlayer;
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
			source = mod.ReportedPlayer;

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

	[HarmonyPatch(typeof(OfficerShootButton), "OnClick")]
	[HarmonyPrefix]
	public static bool OfficerShootVesselPrefix(OfficerShootButton __instance)
	{
		var Target = __instance.Target;
		if (Target == null)
		{
			return true;
		}

		if (Target.HasModifier<FirstDeadShield>())
		{
			return true;
		}

		if (Target.HasModifier<BaseShieldModifier>())
		{
			return true;
		}

		if (!Target.TryGetModifier<VesselPossessedModifier>(out var mod))
		{
			return true;
		}

		var options = OptionGroupSingleton<OfficerOptions>.Instance;
		var alignment = Target.Data.Role.GetRoleAlignment();
		var hasKilled = PossessionHistory.VesselStats.TryGetValue(Target.PlayerId, out var stats) &&
						(stats.GhostCorrectKills > 0 || stats.GhostIncorrectKills > 0) ||
						PossessionHistory.GhostVesselKills.Any(x =>
							x.VesselId == Target.PlayerId && x.VictimId != Target.PlayerId);
		var evilOfficer = (PlayerControl.LocalPlayer.TryGetModifier<AllianceGameModifier>(out var allyMod) &&
							!allyMod.GetsPunished);

		if (options.CanOnlyShootActiveKillers.Value)
		{
			if (!evilOfficer && Target.IsCrewmate() && options.CrewKillingAreInnocent.Value || !hasKilled)
			{
				__instance.CallMisfire();
			}
			else
			{
				__instance.CallShoot();
			}
		}
		else if (!(Target.TryGetModifier<AllianceGameModifier>(out var allyMod2) && !allyMod2.GetsPunished))
		{
			var safeNeutral = options.NonKillingNeutralsAreInnocent.Value &&
							  alignment is RoleAlignment.NeutralBenign
								  or RoleAlignment.NeutralEvil or RoleAlignment.NeutralOutlier;
			if (safeNeutral || !evilOfficer && Target.IsCrewmate())
			{
				__instance.CallMisfire();
			}
			else
			{
				__instance.CallShoot();
			}
		}
		else
		{
			__instance.CallShoot();
		}

		if (!OptionGroupSingleton<OfficerOptions>.Instance.CanSelfReport.Value)
		{
			Coroutines.Start(__instance.CallCoSetBodyReportable(Target.PlayerId));
		}

		return false;
	}
}

[HarmonyPatch]
public static class OfficerShootReversePatch
{
	[HarmonyReversePatch]
	[HarmonyPatch(typeof(OfficerShootButton), "Shoot")]
	public static void CallShoot(this OfficerShootButton instance)
	{
	}

	[HarmonyReversePatch]
	[HarmonyPatch(typeof(OfficerShootButton), "Misfire")]
	public static void CallMisfire(this OfficerShootButton instance)
	{
	}

	[HarmonyPatch(typeof(OfficerShootButton), "CoSetBodyReportable")]
	public static IEnumerator CallCoSetBodyReportable(this OfficerShootButton instance, byte bodyId)
	{
		throw new System.NotImplementedException();
	}
}
