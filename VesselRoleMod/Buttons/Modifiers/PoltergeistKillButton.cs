using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistKillButton : PoltergeistTargetButton<PoltergeistModifier, PlayerControl>, IDiseaseableButton, IKillButton
{
	public override string Name => "Kill";
	public override BaseKeybind Keybind => Keybinds.PrimaryAction;
	public override float Cooldown => PlayerControl.LocalPlayer.GetKillCooldown();
	public override LoadableAsset<Sprite> Sprite => TouAssets.KillSprite;
	public override int MaxUses => OnKill == VesselOnKillType.CannotKill ? 1 : -1;
	public override bool ZeroIsInfinite { get; set; }

	private static VesselOnKillType OnKill => OptionGroupSingleton<VesselOptions>.Instance.KillingGhostOnKill.Value;

	protected override bool CanUseAbility()
	{
		return OptionGroupSingleton<VesselOptions>.Instance.KillingGhostsCanKill &&
			   Role!.HasKillingAbility();
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

	public override void SetActive(bool visible, RoleBehaviour role)
	{
		if (visible)
		{
			SetTimer(InitialCooldown);
		}
		base.SetActive(visible, role);
	}

	protected override void OnClick()
	{
		PlayerControl.LocalPlayer.RpcFramedMurder(
			Target!,
			Vessel!,
			causeOfDeath: "VesselPossession");

		if (OnKill == VesselOnKillType.CannotPossess)
		{
			VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, Vessel!, true);
		}
	}
}
