using HarmonyLib;
using MiraAPI.Modifiers;
using TownOfUs.Modules;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;

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
}
