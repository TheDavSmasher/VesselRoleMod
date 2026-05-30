using HarmonyLib;
using MiraAPI.Modifiers;
using VesselRoleMod.Modifiers.Ghost;

namespace VesselRoleMod.Patches;

[HarmonyPatch]
public static class ButtonClickPatches
{
	[HarmonyPatch(typeof(UseButton), nameof(UseButton.DoClick))]
	[HarmonyPriority(799)] // second
	[HarmonyPrefix]
	public static bool VanillaButtonChecks()
	{
		if (PlayerControl.LocalPlayer != null &&
			PlayerControl.LocalPlayer.TryGetModifier<PoltergeistModifier>(out var mod) && mod.Vessel != null)
		{
			return true;
		}

		return true;
	}
}
