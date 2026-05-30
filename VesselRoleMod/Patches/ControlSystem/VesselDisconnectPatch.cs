using HarmonyLib;
using MiraAPI.Modifiers;
using TownOfUs.Utilities;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.Components;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Patches.ControlSystem;

[HarmonyPatch(typeof(GameData))]
public static class VesselDisconnectPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(nameof(GameData.HandleDisconnect), typeof(PlayerControl), typeof(DisconnectReasons))]
	public static void Prefix([HarmonyArgument(0)] PlayerControl player)
	{
		if (player == null)
		{
			return;
		}

		if (VesselControlState.IsControlled(player.PlayerId, out var controllerId))
		{
			var controller = MiscUtils.PlayerById(controllerId);
			if (controller != null)
			{
				VesselRole.RpcGhostEndPossession(controller, player);
			}
			else
			{
				VesselControlState.ClearControl(player.PlayerId);

				player.RemoveExistingModifier<VesselPossessedModifier>();
			}
		}

		if (VesselControlState.IsControlling(player.PlayerId, out _))
		{
			if (Minigame.Instance && Minigame.Instance.TryCast<VesselConfirmMinigame>() is { } vcm &&
				vcm.GhostId == player.PlayerId)
			{
				Minigame.Instance.Close();
			}
		}

		if (player.TryGetModifier<PoltergeistModifier>(out var mod2) && mod2.Vessel != null)
		{
			VesselRole.RpcGhostEndPossession(player, mod2.Vessel);
		}

		player.RemoveExistingModifier<VesselPossessedModifier>();
	}
}
