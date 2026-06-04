using HarmonyLib;
using MiraAPI.Modifiers;
using System.Linq;
using TownOfUs.Modules;
using TownOfUs.Utilities;
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

	[HarmonyPatch(typeof(BodyReport))]
	public static class BodyReportPatches
	{
		[HarmonyPatch(nameof(BodyReport.ParseMedicReport))]
		[HarmonyPatch(nameof(BodyReport.ParseForensicReport))]
		[HarmonyPrefix]
		public static void ParseBodyReportPrefix(ref BodyReport br)
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

			br.Killer = VesselRole.GetReportedKiller(Vessel, Ghost);

			return;
		}
	}
}
