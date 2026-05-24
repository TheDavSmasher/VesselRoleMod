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
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Buttons.Modifiers;

[MiraIgnore]
public abstract class PoltergeistTargetButton<TTarget> : TownOfUsTargetButton<TTarget> where TTarget : MonoBehaviour
{
	public override bool UsableInDeath => true;
	public override float InitialCooldown => 0.001f;
	public override float Cooldown => 0.001f;
	public override bool ShouldPauseInVent => false;
	public override Color TextOutlineColor => VesselRoleModColors.Vessel;

	protected static RoleBehaviour Role => PlayerControl.LocalPlayer.GetRoleWhenAlive();
	protected static PoltergeistModifier? Modifier => PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>();
	protected static PlayerControl? Vessel => Modifier?.Vessel;

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   PlayerControl.LocalPlayer.Data.IsDead &&
			   Modifier != null &&
			   Vessel!.Data.Role is VesselRole &&
			   CanUseAbility();
	}

	protected abstract bool CanUseAbility();

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
}
