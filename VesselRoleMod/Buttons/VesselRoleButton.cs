using MiraAPI.PluginLoading;
using TownOfUs.Buttons;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Buttons;

[MiraIgnore]
public abstract class VesselRoleButton<TModifier> : TownOfUsButton where TModifier : IVesselModifier
{
	public override bool UsableInDeath => Modifier is IVesselSeekingModifier;
	public override float InitialCooldown => 0.001f;
	public override float Cooldown => 0.001f;
	public override bool ShouldPauseInVent => false;
	public override Color TextOutlineColor => VesselRoleModColors.Vessel;

	protected static RoleBehaviour Role => PlayerControl.LocalPlayer.GetRoleWhenAlive();
	protected static TModifier? Modifier => PlayerControl.LocalPlayer.GetModifierOfType<TModifier>();
	protected static PlayerControl? Vessel => Modifier?.Vessel;

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   (UsableInDeath == PlayerControl.LocalPlayer.Data.IsDead) &&
			   Modifier != null &&
			   Vessel!.Data.Role is VesselRole &&
			   CanUseAbility();
	}

	protected virtual bool CanUseAbility() => true;

	public override bool IsEffectCancellable() => true;

	public override bool CanUse()
	{
		if (Modifier == null)
		{
			return false;
		}

		if (Modifier is PoltergeistModifier && Vessel != null)
		{
			if (Vessel.Data == null ||
				Vessel.HasDied() ||
				Vessel.Data.Disconnected ||
				!VesselControlState.IsControlled(Vessel.PlayerId, out _))
			{
				VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, Vessel);
				return false;
			}
			if (Vessel.IsInTargetingAnimState())
			{
				return false;
			}
		}

		return base.CanUse();
	}

	public override void ClickHandler()
	{
		if (!CanClick())
		{
			return;
		}

		OnClick();
		Button?.SetDisabled(); // Note: not in base.ClickHandler

		if (EffectActive) // Note: not in base.ClickHandler
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
}
