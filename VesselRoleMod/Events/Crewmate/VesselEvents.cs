using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Modifiers;
using System.Linq;
using TownOfUs.Utilities.Appearances;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Roles.Crewmate;

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
		PlayerControl Player = @event.Player;
		if (@event.AbilityType != VesselAbilityType.AdorciseStart ||
			Player.Data.Role is not VesselRole vessel)
		{
			return;
		}

		var deadPlayers = PlayerControl.AllPlayerControls.ToArray()
			.Where(plr => plr.Data.IsDead && !plr.Data.Disconnected && plr.PlayerId != Player.PlayerId).ToList();

		if (Player.TryGetModifier<VesselBlacklistModifier>(out var blacklist))
		{
			deadPlayers = deadPlayers.Where(x => !blacklist.BlacklistedPlrIds.Contains(x.PlayerId)).ToList();
		}

		var color = Palette.PlayerColors[Player.GetDefaultAppearance().ColorId];
		foreach (var ghost in deadPlayers)
		{
			ghost.AddModifier<PoltergeistArrowModifier>(Player, color);
			ghost.AddModifier<ValidAdorcismGhostModifier>(Player);
		}
	}

	[RegisterEvent]
	public static void VesselEndAdorcismHandler(CustomAbilityEvent<VesselAbilityType> @event)
	{
		if (@event.AbilityType != VesselAbilityType.AdorciseEnd)
		{
			return;
		}

		ClearGhostModifiers(@event.Player);
	}

	[RegisterEvent]
	public static void VesselAdorcismSuccessHandler(CustomAbilityEvent<VesselAbilityType> @event)
	{
		if (@event.AbilityType != VesselAbilityType.AdorcismSuccess ||
			@event.Target is not PlayerControl Target)
		{
			return;
		}

		ClearGhostModifiers(Target);

		// @event.Player is the ghost
		// @event.Target is the Vessel
	}

	private static void ClearGhostModifiers(PlayerControl Player)
	{
		foreach (var ghostArrow in ModifierUtils.GetActiveModifiers<PoltergeistArrowModifier>())
		{
			if (ghostArrow == null)
			{
				continue;
			}

			if (ghostArrow.Owner.PlayerId == Player.PlayerId)
			{
				ghostArrow.Player.RemoveModifier(ghostArrow);
			}
		}

		foreach (var validAdorcismMod in ModifierUtils.GetActiveModifiers<ValidAdorcismGhostModifier>())
		{
			if (validAdorcismMod == null)
			{
				continue;
			}

			if (validAdorcismMod.Target.PlayerId == Player.PlayerId)
			{
				validAdorcismMod.Player.RemoveModifier(validAdorcismMod);
			}
		}
	}
}

public enum VesselAbilityType
{
	AdorciseStart,
	AdorciseEnd,
	AdorcismSuccess
}
