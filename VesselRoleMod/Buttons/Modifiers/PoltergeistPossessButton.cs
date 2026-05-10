using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using System.Linq;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;
using static Reactor.Utilities.Extensions.UnityExtensions;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistPossessButton : TownOfUsTargetButton<PlayerControl>
{
	public override string Name => TouLocale.GetParsed("VesselModGhostPossess", "Possess");
	public override BaseKeybind Keybind => Keybinds.TertiaryAction;
	public override Color TextOutlineColor => TownOfUsColors.ButtonBarry;
	public override float InitialCooldown => 0.01f;
	public override float Cooldown => 0.01f;
	public override float EffectDuration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;
	public override ButtonLocation Location => ButtonLocation.BottomLeft;
	public override LoadableAsset<Sprite> Sprite => TouAssets.BarryButtonSprite;
	public override bool UsableInDeath => true;

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   PlayerControl.LocalPlayer.Data.IsDead &&
			   PlayerControl.LocalPlayer.HasModifier<VesselSeekingModifier>();
	}

	public override void FixedUpdateHandler(PlayerControl playerControl)
	{
		TimerPaused = false;
		if (PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>() is PoltergeistModifier pm &&
			pm.Vessel != null &&
			VesselControlState.IsControlled(pm.Vessel.PlayerId, out _) &&
			VesselControlState.IsInInitialGrace(pm.Vessel.PlayerId))
		{
			TimerPaused = true;
		}

		base.FixedUpdateHandler(playerControl);
	}

	public override bool CanUse()
	{
		if (PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>() is not PoltergeistModifier pm)
		{
			return false;
		}

		if (pm.Vessel != null)
		{
			if (pm.Vessel.Data == null ||
				pm.Vessel.HasDied() ||
				pm.Vessel.Data.Disconnected ||
				!VesselControlState.IsControlled(pm.Vessel.PlayerId, out _))
			{
				VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, pm.Vessel);
				return false;
			}
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return false;
		}

		if (!PlayerControl.LocalPlayer.HasDied())
		{
			return false;
		}

		if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
		{
			return false;
		}

		if (!PlayerControl.LocalPlayer.CanMove ||
			PlayerControl.LocalPlayer.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
		{
			return false;
		}

		var newTarget = GetTarget();
		if (newTarget != Target)
		{
			SetOutline(false);
		}

		Target = IsTargetValid(newTarget) ? newTarget : null;
		SetOutline(true);

		return Target != null &&
			((EffectActive && Timer <= EffectDuration - 5f) ||
			(!EffectActive && Timer <= 0));
	}

	public override bool CanClick()
	{
		return (!EffectActive && Timer <= 0 || EffectActive) && CanUse() && Target != null;
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
				validTargetIds.Contains(plr.PlayerId));
	}

	public override void SetOutline(bool active)
	{
		if (Target != null && PlayerControl.LocalPlayer.HasDied())
		{
			Target.cosmetics.currentBodySprite.BodySprite.SetOutline(active ? VesselRoleModColors.Vessel : null);
		}
	}

	public override void OnEffectEnd()
	{
		base.OnEffectEnd();

		OverrideName(TouLocale.Get("VesselModGhostPossess", "Possess"));
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

				VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, pm.Vessel);
				OverrideName(TouLocale.Get("VesselModGhostPossess", "Possess"));
				ResetCooldownAndOrEffect();
				return;
			}
		}

		if (Target == null || Target.Data.Role is not VesselRole)
		{
			return;
		}

		VesselRole.RpcGhostPossession(PlayerControl.LocalPlayer, Target);
		OverrideName(TouLocale.Get("VesselModGhostRelease", "Release"));
		EffectActive = true;
		Timer = EffectDuration;
	}
}
