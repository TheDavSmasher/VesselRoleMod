using MiraAPI.LocalSettings;
using MiraAPI.PluginLoading;
using TownOfUs;
using TownOfUs.Buttons;
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

	protected static TModifier? Modifier => PlayerControl.LocalPlayer.GetModifierOfType<TModifier>();

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   (UsableInDeath == PlayerControl.LocalPlayer.Data.IsDead) &&
			   Modifier?.Vessel.Data.Role is VesselRole &&
			   CanUseAbility();
	}

	protected virtual bool CanUseAbility() => true;

	public override bool IsEffectCancellable() => true;

	public override bool CanUse()
	{
		if (Modifier?.Vessel == null)
		{
			return false;
		}

		if (Modifier is PoltergeistModifier)
		{
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
		}

		return base.CanUse();
	}

	public override void ClickHandler()
	{
		if (!CanClick())
		{
			return;
		}

		if (LimitedUses)
		{
			UsesLeft--;
			Button?.SetUsesRemaining(UsesLeft);
			TownOfUsColors.UseBasic = false;
			if (TextOutlineColor != Color.clear)
			{
				SetTextOutline(TextOutlineColor);
				Button?.usesRemainingSprite.color = TextOutlineColor;
			}

			TownOfUsColors.UseBasic = LocalSettingsTabSingleton<TownOfUsLocalRoleSettings>.Instance
				.UseCrewmateTeamColorToggle.Value;
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
