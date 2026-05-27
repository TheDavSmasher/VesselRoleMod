using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Player;
using System.Linq;
using VesselRoleMod.Modules;

namespace VesselRoleMod.Events;

public static class VesselModEventHandlers
{
	[RegisterEvent]
	public static void PlayerLeaveEventHandler(PlayerLeaveEvent @event)
	{
		if (!MeetingHud.Instance)
		{
			return;
		}

		var player = @event.ClientData.Character;

		if (!player)
		{
			return;
		}

		var pva = MeetingHud.Instance.playerStates.First(x => x.TargetPlayerId == player.PlayerId);

		if (!pva)
		{
			return;
		}

		BlockedMeetingMenu.Instances.Do(x => x.HideSingle(player.PlayerId));
	}
}
