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
	public static void VesselStartAdorcismHandler(CustomAbilityEvent<VesselAbilityType> @event)
	{
		if (@event.AbilityType != VesselAbilityType.AdorciseStart)
		{
			return;
		}


	}

	[RegisterEvent]
	public static void VesselEndAdorcismHandler(CustomAbilityEvent<VesselAbilityType> @event)
	{
		if (@event.AbilityType != VesselAbilityType.AdorciseEnd)
		{
			return;
		}


	}

	[RegisterEvent]
	public static void VesselAdorcismSuccessHandler(CustomAbilityEvent<VesselAbilityType> @event)
	{
		if (@event.AbilityType != VesselAbilityType.AdorcismSuccess)
		{
			return;
		}


	}
}

public enum VesselAbilityType
{
	AdorciseStart,
	AdorciseEnd,
	AdorcismSuccess
}
