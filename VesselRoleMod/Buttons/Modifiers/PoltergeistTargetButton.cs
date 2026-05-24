using HarmonyLib;
using MiraAPI.Modifiers;
using MiraAPI.PluginLoading;
using Reactor.Utilities.Extensions;
using System.Linq;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modules;
using TownOfUs.Roles.Other;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Buttons.Modifiers;

[MiraIgnore]
public abstract class PoltergeistTargetButton<TModifier, TTarget> : TownOfUsTargetButton<TTarget> where TModifier : VesselSeekingModifier
																								  where TTarget : MonoBehaviour
{
	public override bool UsableInDeath => true;
	public override float InitialCooldown => 0.001f;
	public override float Cooldown => 0.001f;
	public override bool ShouldPauseInVent => false;
	public override Color TextOutlineColor => VesselRoleModColors.Vessel;

	protected static RoleBehaviour Role => PlayerControl.LocalPlayer.GetRoleWhenAlive();
	protected static TModifier? Modifier => PlayerControl.LocalPlayer.GetModifier<TModifier>();
	protected static PlayerControl? Vessel => Modifier?.Vessel;

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   PlayerControl.LocalPlayer.Data.IsDead &&
			   Modifier != null &&
			   Vessel!.Data.Role is VesselRole &&
			   CanUseAbility();
	}

	protected virtual bool CanUseAbility() => true;

	public override void SetActive(bool visible, RoleBehaviour role)
	{
		if (!visible)
		{
			SetOutline(false);
		}
		base.SetActive(visible, role);
	}

	public override void SetOutline(bool active)
	{
		if (Target != null && PlayerControl.LocalPlayer.HasDied())
		{
			if (Target is PlayerControl target)
			{
				target.cosmetics.currentBodySprite.BodySprite.SetOutline(active ? VesselRoleModColors.Vessel : null);
			}
			else if (Target is DeadBody body)
			{
				body.bodyRenderers.Do(x => x.SetOutline(active ? VesselRoleModColors.Vessel : null));
			}
			else if (Target is Vent vent)
			{
				vent.SetOutline(active, true, VesselRoleModColors.Vessel);
			}
		}
	}

	public override bool IsTargetValid(TTarget? target)
	{
		if (target is PlayerControl playerTarget)
		{
			return base.IsTargetValid(target) && !playerTarget.inVent &&
				   !playerTarget.GetModifiers<DisabledModifier>().Any(mod => !mod.CanBeInteractedWith) &&
				   !SpectatorRole.TrackedSpectators.Contains(playerTarget.Data.PlayerName);
		}

		return base.IsTargetValid(target);
	}

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
