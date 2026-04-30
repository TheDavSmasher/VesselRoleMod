using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using System.Linq;
using TownOfUs.Utilities.Appearances;
using VesselRoleMod.Buttons.Crewmate;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
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
	public static void RoundStartHandler(RoundStartEvent @event)
	{
		if (@event.TriggeredByIntro)
		{
			return;
		}

		var button = CustomButtonSingleton<VesselAdorciseButton>.Instance;
		button.ResetCooldownAndOrEffect();
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
			VesselRole.RpcSeekVessel(ghost, Player);
			ghost.AddModifier<PoltergeistArrowModifier>(Player, color);
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
			@event.Target is not PlayerControl Target || 
			Target.Data.Role is not VesselRole)
		{
			return;
		}

		ClearGhostModifiers(Target);
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

			if (validAdorcismMod.Vessel.PlayerId == Player.PlayerId)
			{
				VesselRole.RpcVesselClosed(validAdorcismMod.Player, Player);
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
