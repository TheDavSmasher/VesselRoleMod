using HarmonyLib;
using MiraAPI.Modifiers;
using TownOfUs.Utilities;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Patches.ControlSystem;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
[HarmonyPriority(Priority.Low)]
public static class VesselOverlayPatch
{
	[HarmonyPostfix]
	public static void HudManagerUpdatePostfix()
	{
		var local = PlayerControl.LocalPlayer;
		if (local == null)
		{
			return;
		}

		if (local.TryGetModifier<VesselPossessedModifier>(out var mod))
		{
			if (VesselControlState.IsControlled(local.PlayerId, out var ghostId))
			{
				if (MeetingHud.Instance == null &&
					ExileController.Instance == null &&
					local.Data != null &&
					!local.Data.Disconnected &&
					!local.Data.IsDead)
				{
					var ghost = MiscUtils.PlayerById(ghostId);
					if (ghost?.Data != null && !ghost.Data.Disconnected && ghost.HasDied())
					{
						return;
					}
				}

				VesselControlState.ClearControl(local.PlayerId);
			}

			mod.ClearNotification();
			mod.RemoveSelf();
		}
	}
}
