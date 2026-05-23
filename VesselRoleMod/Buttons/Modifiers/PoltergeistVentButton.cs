using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using Rewired;
using System.Linq;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Buttons.Crewmate;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Ghost;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistVentButton : TownOfUsTargetButton<Vent>
{
	public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.VentLabel, "Vent");
	public override bool UsableInDeath => true;
	public override BaseKeybind Keybind => Keybinds.VentAction;
	public override float InitialCooldown => 0.001f;
	public override float Cooldown => Role is EngineerRole
		? CustomButtonSingleton<EngineerVentButton>.Instance.Cooldown : 0.001f;
	public override float EffectDuration => Role is EngineerRole
		? CustomButtonSingleton<EngineerVentButton>.Instance.EffectDuration : 0f;
	public override LoadableAsset<Sprite> Sprite => TouAssets.VentSprite;
	public override Color TextOutlineColor => VesselRoleModColors.Vessel;

	private static RoleBehaviour Role => PlayerControl.LocalPlayer.GetRoleWhenAlive();
	private static PlayerControl? Vessel => PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>()?.Vessel;

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   PlayerControl.LocalPlayer.Data.IsDead &&
			   PlayerControl.LocalPlayer.TryGetModifier<PoltergeistModifier>(out var mod) &&
			   mod.CanVent() == true;
	}

	public override Vent? GetTarget()
	{
		return TouRoleUtils.GetClosestUsableVent(true);
	}

	public override void SetOutline(bool active)
	{
		if (Target != null && PlayerControl.LocalPlayer.HasDied())
		{
			Target.SetOutline(active, true, VesselRoleModColors.Vessel);
		}
	}

	public override bool CanUse()
	{
		var newTarget = GetTarget();
		if (newTarget != Target)
		{
			Target?.SetOutline(false, false);
		}

		Target = IsTargetValid(newTarget) ? newTarget : null;
		SetOutline(true);

		if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
		{
			return false;
		}

		if (PlayerControl.LocalPlayer.HasModifier<GlitchHackedModifier>() || PlayerControl.LocalPlayer
				.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
		{
			return false;
		}

		return (Timer <= 0 && !PlayerControl.LocalPlayer.inVent && Target != null) ||
				PlayerControl.LocalPlayer.inVent;
	}

	public override void ClickHandler()
	{
		if (!CanUse())
		{
			return;
		}

		OnClick();
		Button?.SetDisabled();
		if (EffectActive)
		{
			Timer = Cooldown;
			EffectActive = false;
		}
		else if (HasEffect)
		{
			EffectActive = true;
			Timer = EffectDuration;
		}
		else
		{
			Timer = !PlayerControl.LocalPlayer.inVent ? 0.001f : Cooldown;
		}
	}

	protected override void OnClick()
	{
		throw new System.NotImplementedException();
	}

	public override void OnEffectEnd()
	{
		base.OnEffectEnd();
	}
}
