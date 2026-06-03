using HarmonyLib;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Patches.ControlSystem.Interactions;

[HarmonyPatch]
public static class ControlledCanUsePatches
{
	[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
	[HarmonyPostfix]
	public static void GhostCanUseVentPostfix(NetworkedPlayerInfo pc, ref bool canUse)
	{
		if (pc == null)
		{
			return;
		}

		if (pc.Object.HasModifierOfType<IVesselPossessModifier>())
		{
			canUse &= VesselControlState.HasControl(pc.PlayerId);
		}
	}

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

	[HarmonyPatch(typeof(OptionsConsole), nameof(OptionsConsole.CanUse))] // Lobby only (I think?)
	[HarmonyPatch(typeof(SystemConsole), nameof(SystemConsole.CanUse))] // Cams + Others (I think?)
	[HarmonyPatch(typeof(MapConsole), nameof(MapConsole.CanUse))] // Admin Table
	[HarmonyPrefix]
	public static bool VesselCanUseConsolePrefix(NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse)
	{
		if (pc == null)
		{
			return true;
		}

		if (pc.Object.HasModifierOfType<IVesselPossessModifier>())
		{
			canUse = false;
			couldUse = false;
			return false;
		}

		return true;
	}
}
