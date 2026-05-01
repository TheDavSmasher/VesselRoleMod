using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using VesselRoleMod.Buttons.Crewmate;
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
	public static void RoundStartHandler(RoundStartEvent @event)
	{
		if (@event.TriggeredByIntro)
		{
			return;
		}

		var button = CustomButtonSingleton<VesselAdorciseButton>.Instance;
		button.ResetCooldownAndOrEffect();
	}
}

public enum VesselAbilityType
{
	PoltergeistPossess,
	PoltergeistPossessKill,
	AdorciseStart,
	AdorciseEnd,
	AdorcismSuccess
}
