using HarmonyLib;
using MiraAPI.Modifiers;
using VesselRoleMod.Modifiers.Ghost;

namespace VesselRoleMod.Patches;

[HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetPlayerSpeedMod))]
[HarmonyPriority(Priority.Last)]
public static class PlayerSpeedPatch
{
	public static void Postfix(PlayerControl pc, ref float __result)
	{
		if (pc.TryGetModifier<PoltergeistModifier>(out var mod) && mod.Vessel)
		{
			var vessel = mod.Vessel.MyPhysics;
			__result = vessel.SpeedMod * vessel.Speed / vessel.GhostSpeed;
		}
	}
}