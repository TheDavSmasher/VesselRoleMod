using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Modifiers;
using VesselRoleMod.Modifiers.Crewmate;

namespace VesselRoleMod.Events.Crewmate;

public static class VesselEvents
{
	[RegisterEvent]
	public static void VotingCompleteHandler(VotingCompleteEvent @event)
	{
		ModifierUtils.GetActiveModifiers<VesselBlacklistModifier>().Do(x => x.OnVotingComplete());
	}

	[RegisterEvent]
	public static void VesselAdorcismHandler(CustomAbilityEvent<VesselAbilityType> @event)
	{
		if (@event.AbilityType != VesselAbilityType.Adorcise)
		{
			return;
		}


	}
}

public enum VesselAbilityType
{
	Adorcise
}
