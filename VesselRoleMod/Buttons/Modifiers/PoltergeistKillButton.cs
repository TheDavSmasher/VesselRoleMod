using InnerNet;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities.Extensions;
using System.Linq;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Networking;
using TownOfUs.Roles.Other;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistKillButton : TownOfUsTargetButton<PlayerControl>, IDiseaseableButton, IKillButton
{
	public override string Name => "Kill";
	public override bool UsableInDeath => true;
	public override float InitialCooldown => 0.001f;
	public override BaseKeybind Keybind => Keybinds.PrimaryAction;
	public override float Cooldown => Options.KillingGhostOnKill == VesselOnKillType.None
		? PlayerControl.LocalPlayer.GetKillCooldown()
		: 0.001f;
	public override bool ShouldPauseInVent => false;
	public override Color TextOutlineColor => VesselRoleModColors.Vessel;
	public override LoadableAsset<Sprite> Sprite => TouAssets.KillSprite;
	public override int MaxUses => Options.KillingGhostOnKill != VesselOnKillType.None ? 1 : -1;
	public override bool ZeroIsInfinite { get; set; }

	private static VesselOptions Options => OptionGroupSingleton<VesselOptions>.Instance;
	private static PoltergeistModifier? Modifier => PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>();

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   PlayerControl.LocalPlayer.Data.IsDead &&
			   Modifier?.Vessel.Data.Role is VesselRole &&
			   OptionGroupSingleton<VesselOptions>.Instance.KillingGhostsCanKill &&
			   ((IVesselModifier)Modifier!).Role.HasKillingAbility();
	}

	public override PlayerControl? GetTarget()
	{
		return Modifier?.Vessel.GetClosestLivingPlayer(
			true,
			Distance,
			predicate: plr =>
				plr != null &&
				plr != PlayerControl.LocalPlayer &&
				!plr.HasDied() &&
				!plr.IsInTargetingAnimState());
	}

	public override bool IsTargetValid(PlayerControl? target)
	{
		if (target is PlayerControl playerTarget)
		{
			return base.IsTargetValid(target) && !playerTarget.inVent &&
				   !playerTarget.GetModifiers<DisabledModifier>().Any(mod => !mod.CanBeInteractedWith) &&
				   !SpectatorRole.TrackedSpectators.Contains(playerTarget.Data.PlayerName);
		}

		return base.IsTargetValid(target);
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
		else if (AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started)
		{
			SetOutline(false);
		}
		base.SetActive(visible, role);
	}

	public override void SetOutline(bool active)
	{
		if (Target != null && PlayerControl.LocalPlayer.HasDied())
		{
			Target.cosmetics.currentBodySprite.BodySprite.SetOutline(active ? VesselRoleModColors.Vessel : null);
		}
	}

	public override bool CanUse()
	{
		if (Modifier?.Vessel == null)
		{
			return false;
		}

		if (Modifier.Vessel.Data == null ||
			Modifier.Vessel.HasDied() ||
			Modifier.Vessel.Data.Disconnected ||
			!VesselControlState.IsControlled(Modifier.Vessel.PlayerId, out _))
		{
			VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, Modifier.Vessel);
			return false;
		}
		if (Modifier.Vessel.IsInTargetingAnimState())
		{
			return false;
		}

		return base.CanUse();
	}

	protected override void OnClick()
	{
		Button?.SetDisabled();

		PlayerControl.LocalPlayer.RpcFramedMurder(
			Target!,
			Modifier!.Vessel,
			causeOfDeath: "VesselPossession");

		if (Options.KillingGhostOnKill == VesselOnKillType.CannotPossess)
		{
			VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, Modifier!.Vessel, true);
		}
	}
}
