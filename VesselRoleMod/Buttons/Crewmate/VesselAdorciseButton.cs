using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules.Localization;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Buttons.Crewmate;

public class VesselAdorciseButton : TouRoleTriggerButton<VesselRole>
{
	public override string Name => TouLocale.GetParsed("VesselRoleAdorcise", "Adorcise");
	public override BaseKeybind? Keybind => Keybinds.SecondaryAction;
	public override Color TextOutlineColor => TownOfUsColors.Impostor;
	public override float Cooldown => Math.Clamp(OptionGroupSingleton<VesselOptions>.Instance.AdorciseCooldown + MapCooldown, 5f, 120f);
	public override float EffectDuration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;
	public override float TriggerWindow => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;
	public override LoadableAsset<Sprite> Sprite => VesselCrewAssets.AdorciseSprite;

	public override void ClickHandler()
	{
		if (!CanUse())
		{
			return;
		}

		OnClick();
		Button?.SetDisabled();
		if (HasTrigger && !WaitingOnTrigger)
		{
			WaitingOnTrigger = true;
			Timer = TriggerWindow;
		}
		else if (HasEffect && EffectActive)
		{
			EffectActive = false;
			Timer = Cooldown;
		}
		else
		{
			Timer = Cooldown;
		}
	}

	public override bool CanUse()
	{
		if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
		{
			return false;
		}

		if (PlayerControl.LocalPlayer.HasModifier<GlitchHackedModifier>() || PlayerControl.LocalPlayer
				.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
		{
			return false;
		}

		return ((Timer <= 0 && !EffectActive) ||
			(EffectActive && Timer <= EffectDuration - 5f) ||
			(WaitingOnTrigger && Timer <= TriggerWindow - 2f));
	}

	protected override void OnClick()
	{
		if (EffectActive)
		{
			// TODO: Rpc Exorcise Method
			return;
		}

		if (WaitingOnTrigger && PlayerControl.LocalPlayer.HasModifier<VesselAdorcismModifier>())
		{
			// TODO: Rpc Cancel Adorcise Method
			return;
		}

		// TODO: Rpc Start Adorcise Method
	}

	public override void OnTriggerActivate()
	{
		base.OnTriggerActivate();

		// TODO: Rpc Possess Method
		OverrideName(TouLocale.Get("VesselRoleExorcise", "Exorcise"));
		OverrideSprite(VesselCrewAssets.ExorciseSprite.LoadAsset());
	}

	public override void OnTriggerEnd()
	{
		base.OnTriggerEnd();

		// TODO: Rpc Cancel Adorcise Method
		OverrideName(TouLocale.Get("VesselRoleAdorcise", "Adorcise"));
		OverrideSprite(VesselCrewAssets.AdorciseSprite.LoadAsset());
	}

	public override void OnEffectEnd()
	{
		base.OnEffectEnd();

		// TODO: Rpc Exorcise Method
		OverrideName(TouLocale.Get("VesselRoleAdorcise", "Adorcise"));
		OverrideSprite(VesselCrewAssets.AdorciseSprite.LoadAsset());
	}
}
