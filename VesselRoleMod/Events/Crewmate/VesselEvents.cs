using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TownOfUs.Assets;
using TownOfUs.Utilities;
using UnityEngine;
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

		var notif1 = Helpers.CreateAndShowNotification(
			"Adorcism Started",
			Color.white, new Vector3(0f, 1f, -20f), spr: TouRoleIcons.Medium.LoadAsset());
		notif1.AdjustNotification();
	}

	[RegisterEvent]
	public static void VesselEndAdorcismHandler(CustomAbilityEvent<VesselAbilityType> @event)
	{
		if (@event.AbilityType != VesselAbilityType.AdorciseEnd)
		{
			return;
		}

		var notif1 = Helpers.CreateAndShowNotification(
			"Adorcism Window Ended without Possession",
			Color.white, new Vector3(0f, 1f, -20f), spr: TouRoleIcons.Medium.LoadAsset());
		notif1.AdjustNotification();
	}

	[RegisterEvent]
	public static void VesselAdorcismSuccessHandler(CustomAbilityEvent<VesselAbilityType> @event)
	{
		if (@event.AbilityType != VesselAbilityType.AdorcismSuccess)
		{
			return;
		}

		var notif1 = Helpers.CreateAndShowNotification(
			"Adorcism Succeeded with Possession",
			Color.white, new Vector3(0f, 1f, -20f), spr: TouRoleIcons.Medium.LoadAsset());
		notif1.AdjustNotification();
	}
}

public enum VesselAbilityType
{
	AdorciseStart,
	AdorciseEnd,
	AdorcismSuccess
}
