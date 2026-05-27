using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using System.Collections.Generic;
using System.Linq;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Modifiers.Crewmate;

public sealed class VesselBlacklistModifier : BaseModifier
{
	private MeetingMenu? meetingMenu;

	public override string ModifierName => "VesselBlacklist";

	[HideFromIl2Cpp] public HashSet<byte> BlacklistedPlrIds { get; } = [];

	private Dictionary<byte, SpriteRenderer> ButtonSprites { get; } = [];

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

			MeetingHud.Instance.playerStates.ToList().ForEach(x => GenSpriteRefs(x));

			foreach (var blockPlrId in BlacklistedPlrIds)
			{
				meetingMenu.Actives[blockPlrId] = true;
			}

			foreach (var blockKiller in ModifierUtils.GetActiveModifiers<GhostKillerBlockModifier>(m => m.VesselOwner))
			{
				meetingMenu.Actives[blockKiller.Player.PlayerId] = true;
			}
		}
	}

	private void GenSpriteRefs(PlayerVoteArea voteArea)
	{
		if (voteArea.transform.parent.FindRecursive(t => t.name.Contains("MeetingButton")) is not { } button)
		{
			return;
		}

		var targetBox = button.gameObject;
		var renderer = targetBox.GetComponent<SpriteRenderer>();
		ButtonSprites.Add(voteArea.TargetPlayerId, renderer);
	}

	public void OnVotingComplete()
	{
		if (Player.AmOwner)
		{
			meetingMenu?.HideButtons();
			ButtonSprites.Clear();
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

	public void UpdateBlocked()
	{
		foreach (var pair in ButtonSprites)
		{
			if (!pair.Value)
			{
				continue;
			}

			NetworkedPlayerInfo player = GameData.Instance.GetPlayerById(pair.Key);
			if (!player.Object.HasModifier<GhostKillerBlockModifier>(m => m.VesselOwner))
			{
				continue;
			}

			pair.Value.sprite = VesselModAssets.VesselBlockedSprite.LoadAsset();
			pair.Value.color = Palette.DisabledGrey;
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

		NetworkedPlayerInfo player = GameData.Instance.GetPlayerById(voteArea.TargetPlayerId);
		if (player.Object.HasModifier<GhostKillerBlockModifier>(m => m.VesselOwner))
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
