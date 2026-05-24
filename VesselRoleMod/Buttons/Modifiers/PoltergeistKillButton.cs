using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities.Extensions;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistKillButton : PoltergeistTargetButton<PlayerControl>, IDiseaseableButton, IKillButton
{
	public override string Name => "Kill";
	public override BaseKeybind Keybind => Keybinds.PrimaryAction;
	public override float Cooldown => 0.001f;
	public override float EffectDuration => PlayerControl.LocalPlayer.GetKillCooldown();
	public override LoadableAsset<Sprite> Sprite => TouAssets.KillSprite;

	protected override bool CanUseAbility()
	{
		return Modifier!.CanKill();
	}

	public override PlayerControl? GetTarget()
	{
		return Vessel?.GetClosestLivingPlayer(
			true,
			Distance,
			predicate: plr =>
			    plr != null &&
				plr != PlayerControl.LocalPlayer &&
				!plr.HasDied() &&
				!plr.IsInTargetingAnimState());
	}

	public void SetDiseasedTimer(float multiplier)
	{
		SetTimer(Cooldown * multiplier);
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
		if (Target == null)
		{
			return;
		}

		PlayerControl.LocalPlayer.RpcFramedMurder(
			Target,
			Vessel!,
			causeOfDeath: "VesselPossession");
	}
}
