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
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistPossessButton : PoltergeistTargetButton<IVesselSeekingModifier, PlayerControl>, IAftermathablePlayerButton, IPossessionButton
{
	public override string Name => TouLocale.GetParsed("VesselModGhostPossess", "Possess");
	public override BaseKeybind Keybind => Keybinds.TertiaryAction;
	public override bool HasEffect => EffectActive;
	public override float EffectDuration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;
	public static float MinDuration => OptionGroupSingleton<VesselOptions>.Instance.MinPossessionLength;
	public override ButtonLocation Location => ButtonLocation.BottomLeft;
	public override LoadableAsset<Sprite> Sprite => VesselCrewAssets.PossessButton;

	public override void FixedUpdateHandler(PlayerControl playerControl)
	{
		TimerPaused = false;
		if (Modifier?.Vessel != null &&
			(VesselControlState.IsPausingTimer(Modifier.Vessel.PlayerId) ||
			 Modifier is PoltergeistModifier &&
			 VesselControlState.IsControlled(Modifier.Vessel.PlayerId, out _) &&
			 VesselControlState.IsInInitialGrace(Modifier.Vessel.PlayerId)))
		{
			TimerPaused = true;
		}

		base.FixedUpdateHandler(playerControl);
	}

	public override bool IsEffectCancellable()
	{
		return Timer <= EffectDuration - MinDuration;
	}

	protected override bool ValidTargetInVent()
	{
		return Modifier is PoltergeistModifier;
	}

	public override PlayerControl? GetTarget()
	{
		if (Modifier?.Vessel == null)
		{
			return null;
		}

		if (Modifier is PoltergeistModifier)
		{
			return Modifier.Vessel;
		}

		var validTargetIds = PlayerControl.LocalPlayer.GetModifiers<ValidAdorcismGhostModifier>().Select(m => m.Vessel.PlayerId);
		return PlayerControl.LocalPlayer.GetClosestLivingPlayer(false, Distance,
			predicate: plr =>
			    plr != null &&
				plr != PlayerControl.LocalPlayer &&
				!plr.HasDied() &&
				!plr.IsInTargetingAnimState() &&
				!plr.HasModifierOfType<IUncontrollable>() &&
				plr.HasModifier<VesselAdorcismModifier>() &&
				validTargetIds.Contains(plr.PlayerId) &&
				!VesselControlState.IsPausingTimer(plr.PlayerId));
	}

	public override void OnEffectEnd()
	{
		base.OnEffectEnd();

		if (Modifier is PoltergeistModifier)
		{
			VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, Modifier.Vessel);
		}

		OverrideName(TouLocale.Get("VesselModGhostPossess", "Possess"));
		OverrideSprite(VesselCrewAssets.PossessButton.LoadAsset());
	}

	protected override void OnClick()
	{
		if (Modifier is PoltergeistModifier)
		{
			if (Modifier.Vessel != null)
			{
				if (!Modifier.Vessel.HasDied() &&
					!Modifier.Vessel.Data.Disconnected &&
					VesselControlState.IsControlled(Modifier.Vessel.PlayerId, out _) &&
					Modifier.Vessel.IsInTargetingAnimState()) // pm.Vessel.inVent
				{
					return;
				}

				ResetCooldownAndOrEffect();
				return;
			}
		}

		if (Target!.Data.Role is not VesselRole)
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
