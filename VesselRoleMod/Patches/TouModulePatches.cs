using HarmonyLib;
using MiraAPI.Modifiers;
using MS.Internal.Xml.XPath;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Patches;

[HarmonyPatch]
public static class TouModulePatches
{
	[HarmonyPatch(typeof(GameHistory))]
	public static class GameHistoryPatches
	{
		[HarmonyPatch(nameof(GameHistory.AddMurder))]
		[HarmonyPostfix]
		public static void AddVesselMurderPostfix(PlayerControl killer, PlayerControl victim)
		{
			if (!killer.Data.IsDead || killer.Data.Disconnected)
			{
				return;
			}

			if (!killer.TryGetModifier<PoltergeistModifier>(out var mod))
			{
				return;
			}

			PossessionHistory.AddMurder(killer, mod.Vessel, victim);
		}

		[HarmonyPatch(nameof(GameHistory.ClearMurder))]
		[HarmonyPostfix]
		public static void ClearVesselMurderPostfix(PlayerControl player)
		{
			PossessionHistory.ClearMurder(player);
		}

		[HarmonyPatch(nameof(GameHistory.ClearAll))]
		[HarmonyPostfix]
		public static void ClearAllPostfix()
		{
			PossessionHistory.ClearAll();
		}
	}

	[HarmonyPatch]
	public static class BodyReportPatches
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(BodyReport), nameof(BodyReport.ParseMedicReport));
			yield return AccessTools.Method(typeof(BodyReport), nameof(BodyReport.ParseForensicReport));
		}

		public readonly record struct ReportState(byte VesselId, bool Reset);

		public static void Prefix(ref BodyReport br, ref ReportState __state)
		{
			if (br.Body == null)
			{
				return;
			}

			var deadPlayerId = br.Body.PlayerId;
			var matches = PossessionHistory.GhostVesselKills.Where(x => x.VictimId == deadPlayerId).ToArray();

			PossessionKill? killer = null;

			if (matches.Length > 0)
			{
				killer = matches[0];
			}

			if (killer == null ||
				MiscUtils.PlayerById(killer.KillerId) is not { } Ghost ||
				MiscUtils.PlayerById(killer.VesselId) is not { } Vessel)
			{
				return;
			}

			if (Vessel.TryGetModifier<VesselPossessedModifier>(out var mod))
			{
				mod.ShowCurrentAsCached();
				__state = new(Vessel.PlayerId, true);
			}

			br.Killer = VesselRole.GetReportedKiller(Vessel, Ghost);
		}

		public static void Postfix(ReportState __state)
		{
			if (!__state.Reset ||
				MiscUtils.PlayerById(__state.VesselId) is not { } Vessel)
			{
				return;
			}

			if (Vessel.TryGetModifier<VesselPossessedModifier>(out var mod))
			{
				mod.ResetShownCached();
			}
		}
	}
}
