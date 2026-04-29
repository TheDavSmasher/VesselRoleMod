using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities.Extensions;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Crewmate;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistKillButton : TownOfUsTargetButton<PlayerControl>, IDiseaseableButton, IKillButton
{
	public override string Name => "Kill";
	public override BaseKeybind Keybind => Keybinds.PrimaryAction;
	public override float Cooldown => PlayerControl.LocalPlayer.GetKillCooldown();
	public override LoadableAsset<Sprite> Sprite => TouAssets.KillSprite;

	private static PlayerControl? Vessel => PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>()?.Vessel;

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   PlayerControl.LocalPlayer.Data.IsDead &&
			   PlayerControl.LocalPlayer.HasModifier<PoltergeistModifier>();
	}

	public override PlayerControl? GetTarget()
	{
		return Vessel?.GetClosestLivingPlayer(true, Distance);
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
