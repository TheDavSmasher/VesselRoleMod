using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;
using System.Collections.Generic;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules;
using VesselRoleMod.Utilities;
namespace VesselRoleMod.Modifiers.Crewmate;

public sealed class VesselBlacklistModifier : BaseModifier
{
	private BlockedMeetingMenu? meetingMenu;

	public override string ModifierName => "VesselBlacklist";

	[HideFromIl2Cpp] public HashSet<byte> BlacklistedPlrIds { get; } = [];

	public override void OnActivate()
	{
		base.OnActivate();

		if (Player.AmOwner)
		{
			meetingMenu = new BlockedMeetingMenu(
				Player.Data.Role,
				SetBlacklist,
				MeetingAbilityType.Toggle,
				VesselModAssets.VesselBlockedSprite,
				VesselModAssets.VesselUnblockedSprite,
				VesselModAssets.VesselBlockedSprite,
				IsExempt,
				IsBlocked,
				Color.white,
				Color.white)
			{
				Position = new Vector3(-0.40f, 0f, 0f)
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
		this.RemoveSelf();
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
		NetworkedPlayerInfo? player = GameData.Instance.GetPlayerById(voteArea.PlayerId);

		return Player.Data.IsDead || voteArea.PlayerId == Player.PlayerId ||
			   !player || !player.Object || player.Object.Data.Disconnected;
	}

	private bool IsBlocked(PlayerVoteArea voteArea)
	{
		NetworkedPlayerInfo? player = GameData.Instance.GetPlayerById(voteArea.PlayerId);

		return player != null && player.Object.Data.IsDead && player.Object.HasModifier<GhostKillerBlockModifier>(m => m.VesselOwner);
	}

	private void SetBlacklist(PlayerVoteArea voteArea, MeetingHud __instance)
	{
		if (meetingMenu == null || __instance.state == MeetingHud.MeetingStates.Discussion || IsExempt(voteArea) || IsBlocked(voteArea))
		{
			return;
		}

		if (meetingMenu.Actives[voteArea.PlayerId] = !meetingMenu.Actives[voteArea.PlayerId])
		{
			BlacklistedPlrIds.Add(voteArea.PlayerId);
		}
		else
		{
			BlacklistedPlrIds.Remove(voteArea.PlayerId);
		}
	}
}
