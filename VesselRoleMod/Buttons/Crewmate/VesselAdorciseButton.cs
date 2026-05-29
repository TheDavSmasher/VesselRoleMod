using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using System;
using System.Linq;
using TownOfUs;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
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

public class VesselAdorciseButton : TouRoleTriggerButton<VesselRole>, IPossessionButton
{
	public override string Name => TouLocale.GetParsed("VesselRoleAdorcise", "Adorcise");
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
		if (PlayerControl.LocalPlayer.HasModifier<VesselAdorcismModifier>() &&
			VesselControlState.IsPausingTimer(PlayerControl.LocalPlayer.PlayerId))
		{
			TimerPaused = true;
		}

		base.FixedUpdateHandler(playerControl);
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

		var deadPlayers = PlayerControl.AllPlayerControls.ToArray()
			.Where(plr => plr.Data.IsDead && !plr.Data.Disconnected && plr.PlayerId != PlayerControl.LocalPlayer.PlayerId &&
						  !plr.HasModifier<GhostKillerBlockModifier>() &&
						  (plr.Data.Role is IGhostRole { Caught: true } || plr.Data.Role is not IGhostRole));

		if (PlayerControl.LocalPlayer.TryGetModifier<VesselBlacklistModifier>(out var blacklist))
		{
			deadPlayers = deadPlayers.Where(x => !blacklist.BlacklistedPlrIds.Contains(x.PlayerId));
		}

		if (!OptionGroupSingleton<VesselOptions>.Instance.CanHostImpostors)
		{
			deadPlayers = deadPlayers.Where(x => !x.IsImpostor());
		}

		if (!OptionGroupSingleton<VesselOptions>.Instance.CanHostNeutrals)
		{
			deadPlayers = deadPlayers.Where(x => !x.IsNeutral());
		}

		if (!deadPlayers.Any())
		{
			return;
		}

		if (!PlayerControl.LocalPlayer.HasModifier<VesselAdorcismModifier>())
		{
			PlayerControl.LocalPlayer.RpcAddModifier<VesselAdorcismModifier>();
		}

		foreach (var ghost in deadPlayers)
		{
			VesselRole.RpcSeekVessel(ghost, PlayerControl.LocalPlayer);
		}
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

	public void OnSuccess() => ActivateTriggerEffect();

	public override void OnTriggerActivate()
	{
		base.OnTriggerActivate();

		OverrideName(TouLocale.Get("VesselRoleExorcise", "Exorcise"));
		OverrideSprite(VesselCrewAssets.ExorciseSprite.LoadAsset());
	}

	public override void OnTriggerEnd()
	{
		base.OnTriggerEnd();

		if (PlayerControl.LocalPlayer.GetModifier<VesselAdorcismModifier>() is not { } mod)
		{
			return;
		}
		PlayerControl.LocalPlayer.RpcRemoveModifier<VesselAdorcismModifier>();

		OverrideName(TouLocale.Get("VesselRoleAdorcise", "Adorcise"));
		OverrideSprite(VesselCrewAssets.AdorciseSprite.LoadAsset());
	}

	public override void OnEffectEnd()
	{
		base.OnEffectEnd();

		if (PlayerControl.LocalPlayer.Data.Role is VesselRole &&
			PlayerControl.LocalPlayer.GetModifier<VesselPossessedModifier>() is { } mod)
		{
			VesselRole.RpcGhostEndPossession(mod.Ghost, PlayerControl.LocalPlayer);
		}

		OverrideName(TouLocale.Get("VesselRoleAdorcise", "Adorcise"));
		OverrideSprite(VesselCrewAssets.AdorciseSprite.LoadAsset());
	}
}
