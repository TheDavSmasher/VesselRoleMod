using HarmonyLib;
using TownOfUs.Modules.ControlSystem;
using TownOfUs.Roles.Impostor;
using TownOfUs.Utilities;
using VesselRoleMod.Modifiers.Crewmate;
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
}
