using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Translation;
using MiraAPI.Utilities.Assets;
using System;
using System.Linq;
using TownOfUs;
using TownOfUs.Buttons;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Buttons.Crewmate;

public class VesselAdorciseButton : TouRoleTriggerButton<VesselRole>
{
	public override string Name => MiraLocaleManager.Get("VesselRoleAdorcise", "Adorcise");
	public override BaseKeybind? Keybind => Keybinds.SecondaryAction;
	public override Color TextOutlineColor => TownOfUsColors.Impostor;
	public override float Cooldown => Math.Clamp(OptionGroupSingleton<VesselOptions>.Instance.AdorciseCooldown + MapCooldown, 5f, 120f);
	public override float EffectDuration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;
	public override float MinDuration => OptionGroupSingleton<VesselOptions>.Instance.MinPossessionLength;
	public override float TriggerWindow => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;
	public override LoadableAsset<Sprite> Sprite => VesselCrewAssets.AdorciseSprite;

	public override void FixedUpdateHandler(PlayerControl playerControl)
	{
		TimerPaused = false;
		if ((PlayerControl.LocalPlayer.HasModifier<VesselAdorcismModifier>() &&
			 VesselControlState.IsPausingTimer(PlayerControl.LocalPlayer.PlayerId)) ||
			(PlayerControl.LocalPlayer.HasModifier<VesselPossessedModifier>() &&
			 VesselControlState.IsControlled(PlayerControl.LocalPlayer.PlayerId, out _) &&
			 VesselControlState.IsInInitialGrace(PlayerControl.LocalPlayer.PlayerId)))
		{
			TimerPaused = true;
		}

		base.FixedUpdateHandler(playerControl);
	}

	public override bool IsEffectCancellable()
	{
		return Timer <= EffectDuration - MinDuration;
	}

	public override bool IsTriggerCancellable()
	{
		return Timer <= TriggerWindow - 2f;
	}

	protected override void OnClick()
	{
		if (EffectActive)
		{
			OnEffectEnd();
			return;
		}

		if (WaitingOnTrigger)
		{
			OnTriggerEnd();
			return;
		}

		var deadPlayers = PlayerControl.AllPlayerControls.ToArray().Where(IsValidGhost).ToList();

		if (deadPlayers.Count == 0)
		{
			return;
		}

		VesselRole.RpcSeekVessel(PlayerControl.LocalPlayer, deadPlayers);
	}

	private static bool IsValidGhost(PlayerControl plr)
	{
		if (plr.AmOwner)
		{
			return false;
		}
		if (!plr.Data.IsDead || plr.Data.Disconnected)
		{
			return false;
		}
		if (plr.Data.Role is IGhostRole role && !role.Caught)
		{
			return false;
		}
		if (plr.HasModifier<GhostKillerBlockModifier>())
		{
			return false;
		}
		if (!OptionGroupSingleton<VesselOptions>.Instance.CanHostImpostors && plr.IsImpostor())
		{
			return false;
		}
		if (!OptionGroupSingleton<VesselOptions>.Instance.CanHostNeutrals && plr.IsNeutral())
		{
			return false;
		}
		if (PlayerControl.LocalPlayer.TryGetModifier<VesselBlacklistModifier>(out var blacklist) &&
			blacklist.BlacklistedPlrIds.Contains(plr.PlayerId))
		{
			return false;
		}
		return true;
	}

	public override bool CanUse()
	{
		if (PlayerControl.LocalPlayer.IsInTargetingAnimState())
		{
			return false;
		}

		if (PlayerControl.LocalPlayer.inVent)
		{
			return true;
		}

		return base.CanUse();
	}

	public override void OnTriggerActivate()
	{
		base.OnTriggerActivate();

		OverrideName(MiraLocaleManager.Get("VesselRoleExorcise", "Exorcise"));
		OverrideSprite(VesselCrewAssets.ExorciseSprite.LoadAsset());
	}

	public override void OnTriggerEnd()
	{
		base.OnTriggerEnd();

		if (PlayerControl.LocalPlayer.HasModifier<VesselAdorcismModifier>())
		{
			VesselRole.RpcVesselClosed(PlayerControl.LocalPlayer);
		}

		OverrideName(MiraLocaleManager.Get("VesselRoleAdorcise", "Adorcise"));
		OverrideSprite(VesselCrewAssets.AdorciseSprite.LoadAsset());
	}

	public override void OnEffectEnd()
	{
		base.OnEffectEnd();

		if (PlayerControl.LocalPlayer.Data.Role is VesselRole &&
			PlayerControl.LocalPlayer.TryGetModifier<VesselPossessedModifier>(out var mod))
		{
			VesselRole.RpcGhostEndPossession(mod.Ghost, PlayerControl.LocalPlayer);
		}

		OverrideName(MiraLocaleManager.Get("VesselRoleAdorcise", "Adorcise"));
		OverrideSprite(VesselCrewAssets.AdorciseSprite.LoadAsset());
	}
}
