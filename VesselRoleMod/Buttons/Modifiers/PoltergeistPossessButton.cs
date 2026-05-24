using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using System.Linq;
using TownOfUs.Buttons;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistPossessButton : PoltergeistTargetButton<VesselSeekingModifier, PlayerControl>, IAftermathablePlayerButton
{
	public override string Name => TouLocale.GetParsed("VesselModGhostPossess", "Possess");
	public override BaseKeybind Keybind => Keybinds.TertiaryAction;
	public override float EffectDuration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;
	public static float MinDuration => OptionGroupSingleton<VesselOptions>.Instance.MinPossessionLength;
	public override ButtonLocation Location => ButtonLocation.BottomLeft;
	public override LoadableAsset<Sprite> Sprite => VesselCrewAssets.PossessButton;

	public override void FixedUpdateHandler(PlayerControl playerControl)
	{
		TimerPaused = false;
		if (PlayerControl.LocalPlayer.GetModifier<VesselSeekingModifier>() is { } vm &&
			vm.Vessel != null &&
			(VesselControlState.IsPausingTimer(vm.Vessel.PlayerId) ||
			 vm is PoltergeistModifier pm &&
			 VesselControlState.IsControlled(pm.Vessel.PlayerId, out _) &&
			 VesselControlState.IsInInitialGrace(pm.Vessel.PlayerId)))
		{
			TimerPaused = true;
		}

		base.FixedUpdateHandler(playerControl);
	}

	public override bool IsEffectCancellable()
	{
		return Timer <= EffectDuration - MinDuration;
	}

	public override void ClickHandler()
	{
		if (!CanClick())
		{
			return;
		}

		OnClick();
		Button?.SetDisabled();
	}

	public override PlayerControl? GetTarget()
	{
		if (!PlayerControl.LocalPlayer.HasModifier<VesselSeekingModifier>())
		{
			return null;
		}

		if (PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>() is PoltergeistModifier pm &&
			pm.Vessel != null)
		{
			return pm.Vessel;
		}

		var validTargetIds = PlayerControl.LocalPlayer.GetModifiers<ValidAdorcismGhostModifier>().Select(m => m.Vessel.PlayerId);
		return PlayerControl.LocalPlayer.GetClosestLivingPlayer(false, Distance,
			predicate: plr =>
			    plr != null &&
				plr != PlayerControl.LocalPlayer &&
				!plr.HasDied() &&
				!plr.IsInTargetingAnimState() &&
				!plr.GetModifiers<BaseModifier>().Any(x => x is IUncontrollable) &&
				plr.HasModifier<VesselAdorcismModifier>() &&
				validTargetIds.Contains(plr.PlayerId) &&
				!VesselControlState.IsPausingTimer(plr.PlayerId));
	}

	public override void OnEffectEnd()
	{
		base.OnEffectEnd();

		if (PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>() is PoltergeistModifier pm)
		{
			VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, pm.Vessel);
		}

		OverrideName(TouLocale.Get("VesselModGhostPossess", "Possess"));
		OverrideSprite(VesselCrewAssets.PossessButton.LoadAsset());
	}

	protected override void OnClick()
	{
		if (!PlayerControl.LocalPlayer.HasModifier<VesselSeekingModifier>())
		{
			return;
		}

		if (PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>() is PoltergeistModifier pm)
		{
			if (pm.Vessel != null)
			{
				if (!pm.Vessel.HasDied() &&
					!pm.Vessel.Data.Disconnected &&
					VesselControlState.IsControlled(pm.Vessel.PlayerId, out _) &&
					pm.Vessel.IsInTargetingAnimState()) // pm.Vessel.inVent
				{
					return;
				}

				ResetCooldownAndOrEffect();
				return;
			}
		}

		if (Target == null || Target.Data.Role is not VesselRole)
		{
			return;
		}

		VesselRole.RpcGhostTryPossessing(PlayerControl.LocalPlayer, Target);
	}

	public void OnSuccess()
	{
		OverrideName(TouLocale.Get("VesselModGhostRelease", "Release"));
		OverrideSprite(VesselCrewAssets.ReleaseButton.LoadAsset());
		EffectActive = true;
		Timer = EffectDuration;
	}

	public void AftermathHandler()
	{
		if (!EffectActive)
		{
			Info("Aftermath handling when not possessing");
			return;
		}

		ResetCooldownAndOrEffect();
	}
}
