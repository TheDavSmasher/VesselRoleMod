using HarmonyLib;
using MiraAPI.Modifiers;
using TownOfUs.Roles.Neutral;
using VesselRoleMod.Modifiers.Ghost;

namespace VesselRoleMod.Patches.ControlSystem;

[HarmonyPatch]
public sealed class VesselHauntPatch
{
	[HarmonyPatch(typeof(CrewmateGhostRole), nameof(CrewmateGhostRole.UseAbility))]
	[HarmonyPatch(typeof(ImpostorGhostRole), nameof(ImpostorGhostRole.UseAbility))]
	[HarmonyPatch(typeof(NeutralGhostRole), nameof(NeutralGhostRole.UseAbility))]
	[HarmonyPrefix]
	public static bool GhostPossessionBlockHaunt()
	{
		if (PlayerControl.LocalPlayer != null &&
			PlayerControl.LocalPlayer.Data.IsDead &&
			PlayerControl.LocalPlayer.HasModifier<PoltergeistModifier>())
		{
			return false;
		}

		return true;
	}
}
