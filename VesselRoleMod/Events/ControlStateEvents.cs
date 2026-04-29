using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Modifiers;
using TownOfUs.Events.TouEvents;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modules.ControlSystem;

namespace VesselRoleMod.Events;

public static class ControlStateEvents
{
	[RegisterEvent]
	public static void RoundStartEventHandler(RoundStartEvent @event)
	{
		VesselControlState.ClearAll();

		foreach (var player in PlayerControl.AllPlayerControls)
		{
			if (player.TryGetModifier<PoltergeistModifier>(out var gmod))
			{
				player.RemoveModifier(gmod);
			}

			if (player.TryGetModifier<VesselPossessedModifier>(out var vmod))
			{
				player.RemoveModifier(vmod);
			}
		}
	}

	[RegisterEvent]
	public static void ClientGameEndEventHandler(ClientGameEndEvent @event)
	{
		VesselControlState.ClearAll();

		foreach (var player in PlayerControl.AllPlayerControls)
		{
			if (player.TryGetModifier<PoltergeistModifier>(out var gmod))
			{
				player.RemoveModifier(gmod);
			}

			if (player.TryGetModifier<VesselPossessedModifier>(out var vmod))
			{
				player.RemoveModifier(vmod);
			}
		}
	}
}
