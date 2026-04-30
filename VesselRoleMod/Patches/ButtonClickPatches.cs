using HarmonyLib;
using MiraAPI.Modifiers;
using System.Linq;
using TownOfUs.Modifiers;
using VesselRoleMod.Modifiers.Ghost;
using static TownOfUs.Patches.ButtonClickPatches;

namespace VesselRoleMod.Patches;

[HarmonyPatch]
public static class ButtonClickPatches
{
	[HarmonyPatch(typeof(ReportButton), nameof(ReportButton.DoClick))]
	[HarmonyPatch(typeof(UseButton), nameof(UseButton.DoClick))]
	[HarmonyPatch(typeof(PetButton), nameof(PetButton.DoClick))]
	[HarmonyPriority(Priority.First)]
	[HarmonyPrefix]
	public static bool VanillaButtonChecks(ActionButton __instance)
	{
		if (!CanUseAbilities())
		{
			return false;
		}

		if (PlayerControl.LocalPlayer != null)
		{
			var disabledMods = PlayerControl.LocalPlayer.GetModifiers<DisabledModifier>();
			if (__instance is UseButton)
			{
				if (PlayerControl.LocalPlayer.TryGetModifier<PoltergeistModifier>(out var mod) && mod.Vessel != null)
				{
					return true;
				}
			}

			if (__instance is ReportButton && disabledMods.Any(x => !x.CanReport))
			{
				return false;
			}

			if (disabledMods.Any(x => !x.CanUseConsoles))
			{
				return false;
			}
		}

		return true;
	}
}
