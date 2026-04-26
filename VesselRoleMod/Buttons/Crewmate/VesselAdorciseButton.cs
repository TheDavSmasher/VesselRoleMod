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
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Buttons.Crewmate;

public class VesselAdorciseButton : TownOfUsRoleButton<VesselRole>
{
	public override string Name => TouLocale.GetParsed("VesselRoleAdorcise", "Adorcise");
	public override BaseKeybind? Keybind => Keybinds.SecondaryAction;
	public override Color TextOutlineColor => TownOfUsColors.Impostor;
	public override float Cooldown => Math.Clamp(OptionGroupSingleton<VesselOptions>.Instance.AdorciseCooldown + MapCooldown, 5f, 120f);
	public override float EffectDuration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;
	public override LoadableAsset<Sprite> Sprite => throw new NotImplementedException();

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

		return ((Timer <= 0 && !EffectActive) || (EffectActive && Timer <= EffectDuration - 5f));
	}

	protected override void OnClick()
	{
		if (EffectActive)
		{
			// TODO: Rpc Method
			return;
		}
		
		// TODO: RpcMethod
		OverrideName(TouLocale.Get("VesselRoleExorcise", "Exorcise"));
	}

	public override void OnEffectEnd()
	{
		base.OnEffectEnd();

		// TODO: Rpc Method
		OverrideName(TouLocale.Get("VesselRoleAdorcise", "Adorcise"));
	}
}
