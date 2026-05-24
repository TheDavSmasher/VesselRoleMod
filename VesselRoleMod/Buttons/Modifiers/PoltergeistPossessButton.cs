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
		if (Modifier != null && Vessel != null &&
			(VesselControlState.IsPausingTimer(Vessel.PlayerId) ||
			 Modifier is PoltergeistModifier &&
			 VesselControlState.IsControlled(Vessel.PlayerId, out _) &&
			 VesselControlState.IsInInitialGrace(Vessel.PlayerId)))
		{
			TimerPaused = true;
		}

		base.FixedUpdateHandler(playerControl);
	}

	public override bool IsEffectCancellable()
	{
		return Timer <= EffectDuration - MinDuration;
	}

	public override PlayerControl? GetTarget()
	{
		if (Modifier == null)
		{
			return null;
		}

		if (Modifier is PoltergeistModifier && Vessel != null)
		{
			return Vessel;
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

		if (Modifier is PoltergeistModifier)
		{
			VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, Vessel!);
		}

		OverrideName(TouLocale.Get("VesselModGhostPossess", "Possess"));
		OverrideSprite(VesselCrewAssets.PossessButton.LoadAsset());
	}

	protected override void OnClick()
	{
		if (Modifier is PoltergeistModifier)
		{
			if (Vessel != null)
			{
				if (!Vessel.HasDied() &&
					!Vessel.Data.Disconnected &&
					VesselControlState.IsControlled(Vessel.PlayerId, out _) &&
					Vessel.IsInTargetingAnimState()) // pm.Vessel.inVent
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
