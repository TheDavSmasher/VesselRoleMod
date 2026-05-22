using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;
using System.Collections.Generic;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Assets;

namespace VesselRoleMod.Modifiers.Crewmate;

public sealed class VesselBlacklistModifier : BaseModifier
{
	private MeetingMenu? meetingMenu;

	public override string ModifierName => "VesselBlacklist";

	[HideFromIl2Cpp] public HashSet<byte> BlacklistedPlrIds { get; } = [];

	public override void OnActivate()
	{
		base.OnActivate();

		if (Player.AmOwner)
		{
			meetingMenu = new MeetingMenu(
				Player.Data.Role,
				SetBlacklist,
				MeetingAbilityType.Toggle,
				VesselModAssets.VesselBlockedSprite,
				VesselModAssets.VesselUnblockedSprite,
				IsExempt,
				Color.white,
				Color.white)
			{
				Position = new Vector3(-0.40f, 0f, -3f)
			};
		}
	}

	public override void OnMeetingStart()
	{
		if (Player.AmOwner && meetingMenu != null)
		{
			meetingMenu.GenButtons(MeetingHud.Instance,
				Player.AmOwner && !Player.HasDied() && !Player.HasModifier<JailedModifier>());

			foreach (var blockPlrId in BlacklistedPlrIds)
			{
				meetingMenu.Actives[blockPlrId] = true;
			}
		}
	}

	public void OnVotingComplete()
	{
		if (Player.AmOwner)
		{
			meetingMenu?.HideButtons();
		}
	}

	public override void OnDeath(DeathReason reason)
	{
		ModifierComponent?.RemoveModifier(this);
	}

	public override void OnDeactivate()
	{
		if (Player.AmOwner)
		{
			meetingMenu?.Dispose();
			meetingMenu = null;
		}
	}

	private bool IsExempt(PlayerVoteArea voteArea)
	{
		NetworkedPlayerInfo? player = GameData.Instance.GetPlayerById(voteArea.TargetPlayerId);

		return Player.Data.IsDead || voteArea.TargetPlayerId == Player.PlayerId ||
			   !player || !player.Object || player.Object.Data.Disconnected;
	}

	private void SetBlacklist(PlayerVoteArea voteArea, MeetingHud __instance)
	{
		if (meetingMenu == null || __instance.state == MeetingHud.VoteStates.Discussion || IsExempt(voteArea))
		{
			return;
		}

		if (meetingMenu.Actives[voteArea.TargetPlayerId] = !meetingMenu.Actives[voteArea.TargetPlayerId])
		{
			BlacklistedPlrIds.Add(voteArea.TargetPlayerId);
		}
		else
		{
			BlacklistedPlrIds.Remove(voteArea.TargetPlayerId);
		}
	}
}
