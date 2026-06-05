using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TownOfUs.Events.TouEvents;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Events;

public static class ControlStateEvents
{
	[RegisterEvent]
	public static void RoundStartEventHandler(RoundStartEvent _)
	{
		VesselControlState.ClearAll();

		foreach (var player in PlayerControl.AllPlayerControls)
		{
			player.RemoveExistingModifier<PoltergeistModifier>();
			player.RemoveExistingModifier<VesselPossessedModifier>();
		}
	}

	[RegisterEvent]
	public static void ClientGameEndEventHandler(ClientGameEndEvent _)
	{
		VesselControlState.ClearAll();

		foreach (var player in PlayerControl.AllPlayerControls)
		{
			player.RemoveExistingModifier<PoltergeistModifier>();
			player.RemoveExistingModifier<VesselPossessedModifier>();
		}
	}
}
