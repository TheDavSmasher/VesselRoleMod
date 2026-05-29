using HarmonyLib;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Patches.ControlSystem;

[HarmonyPatch]
public static class ControlledCanUsePatches
{
	[HarmonyPatch(typeof(Console), nameof(Console.FindTask))]
	[HarmonyPrefix]
	public static bool ConsoleFindTaskPrefix(PlayerControl pc, ref PlayerTask __result)
	{
		if (pc == null)
		{
			return true;
		}

		if (pc.HasModifierOfType<IVesselPossessModifier>())
		{
			__result = null!;
			return false;
		}

		return true;
	}
}
