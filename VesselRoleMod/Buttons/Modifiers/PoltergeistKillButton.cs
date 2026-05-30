using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities.Extensions;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistKillButton : TownOfUsTargetButton<PlayerControl>, IDiseaseableButton, IKillButton
{
	public override string Name => "Kill";
	public override bool UsableInDeath => true;
	public override BaseKeybind Keybind => Keybinds.PrimaryAction;
	public override float InitialCooldown => 0.01f;
	public override float Cooldown => 0.01f;
	public override float EffectDuration => PlayerControl.LocalPlayer.GetKillCooldown();
	public override LoadableAsset<Sprite> Sprite => TouAssets.KillSprite;
	public override int MaxUses => OnKill == VesselOnKillType.CannotKill ? 1 : -1;
	public override bool ZeroIsInfinite { get; set; }

	private static VesselOnKillType OnKill => OptionGroupSingleton<VesselOptions>.Instance.KillingGhostOnKill.Value;
	private static PlayerControl? Vessel => PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>()?.Vessel;

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   PlayerControl.LocalPlayer.Data.IsDead &&
			   PlayerControl.LocalPlayer.TryGetModifier<PoltergeistModifier>(out var mod) &&
			   mod.CanKill();
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

		if (OnKill == VesselOnKillType.CannotPossess)
		{
			VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, Vessel!, true);
		}
	}
}
