using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Roles.Crewmate;
using static Reactor.Utilities.Extensions.UnityExtensions;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistPossessButton : TownOfUsTargetButton<PlayerControl>
{
	public override string Name => TouLocale.GetParsed("VesselModGhostPossess", "Possess");
	public override BaseKeybind Keybind => Keybinds.SecondaryAction;
	public override Color TextOutlineColor => TownOfUsColors.ButtonBarry;
	public override float Cooldown => 1f;
	public override ButtonLocation Location => ButtonLocation.BottomLeft;
	public override LoadableAsset<Sprite> Sprite => TouAssets.BarryButtonSprite;
	public override bool UsableInDeath => true;

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   PlayerControl.LocalPlayer.HasModifier<ValidAdorcismGhostModifier>() &&
			   PlayerControl.LocalPlayer.Data.IsDead;
	}

	public override PlayerControl? GetTarget()
	{
		return PlayerControl.LocalPlayer.GetClosestLivingPlayer(false, Distance,
			predicate: x => x.HasModifier<VesselAdorcismModifier>());
	}

	public override void SetOutline(bool active)
	{
		if (Target != null && PlayerControl.LocalPlayer.HasDied())
		{
			Target.cosmetics.currentBodySprite.BodySprite.SetOutline(active ? VesselRoleModColors.Vessel : null);
		}
	}

	protected override void OnClick()
	{
		if (Target == null || Target.Data.Role is not VesselRole Role)
		{
			return;
		}

		EffectActive = true;
		Timer = EffectDuration;

		// Rpc
	}
}
