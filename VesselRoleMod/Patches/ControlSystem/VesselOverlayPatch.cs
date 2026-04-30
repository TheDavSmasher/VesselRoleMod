using HarmonyLib;
using MiraAPI.Modifiers;
using TownOfUs.Utilities;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modules.ControlSystem;

namespace VesselRoleMod.Patches.ControlSystem;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
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

		var hasModifier = local.TryGetModifier<VesselPossessedModifier>(out var mod);
		var isControlled = VesselControlState.IsControlled(local.PlayerId, out var ghostId);

		if (hasModifier && !isControlled)
		{
			mod?.ClearNotification();
			if (mod != null)
				local.RemoveModifier(mod);
			return;
		}

		if (hasModifier)
		{
			var shouldClear =
				MeetingHud.Instance != null ||
				ExileController.Instance != null ||
				local.Data == null ||
				local.Data.Disconnected ||
				local.Data.IsDead;

			if (!shouldClear)
			{
				var ghost = MiscUtils.PlayerById(ghostId);
				if (ghost == null || ghost.Data == null || ghost.Data.Disconnected || !ghost.HasDied())
				{
					shouldClear = true;
				}
			}

			if (shouldClear)
			{
				mod?.ClearNotification();
				if (mod != null)
				{
					VesselControlState.ClearControl(local.PlayerId);
					local.RemoveModifier(mod);
				}
			}
		}
	}
}
