using HarmonyLib;
using VesselRoleMod.Modules;
namespace VesselRoleMod.Patches;

[HarmonyPatch]
public static class BlockMeetingMenuPatches
{
	[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
	[HarmonyPostfix]
	public static void BlockedMeetingMenuUpdatePostfix()
	{
		BlockedMeetingMenu.Instances.Do(x => x.Update());
	}

	[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
	[HarmonyPatch(typeof(TutorialManager), nameof(TutorialManager.Awake))]
	[HarmonyPostfix]
	public static void LobbyStartPatch()
	{
		BlockedMeetingMenu.ClearAll();
	}
}
