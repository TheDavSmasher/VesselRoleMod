using HarmonyLib;
using InnerNet;
using MiraAPI.Modifiers;
using MiraAPI.PluginLoading;
using Reactor.Utilities.Extensions;
using System;
using TownOfUs.Assets;
using TownOfUs.Modifiers;
using TownOfUs.Roles.Other;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers;

namespace VesselRoleMod.Buttons;

[MiraIgnore]
public abstract class PoltergeistTargetButton<TModifier, TTarget> : VesselRoleButton<TModifier> where TModifier : IVesselModifier
																								where TTarget : MonoBehaviour
{
	/// <summary>
	/// Gets or sets the target object of the button.
	/// </summary>
	public TTarget? Target { get; set; }

	/// <summary>
	/// Gets the distance the player must be from the target object to use the button.
	/// </summary>
	public virtual float Distance => PlayerControl.LocalPlayer.Data.Role.GetAbilityDistance();

	public override void CreateButton(Transform parent)
	{
		base.CreateButton(parent);

		if (Button == null)
		{
			return;
		}

		switch (typeof(TTarget))
		{
			case Type t when t == typeof(Vent):
				Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterVentSprite.LoadAsset();
				break;
			case Type t when t == typeof(DeadBody):
				Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterBodySprite.LoadAsset();
				break;
			case Type t when t == typeof(PlayerControl):
				Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterPlayerSprite.LoadAsset();
				break;
			default:
				Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterBasicSprite.LoadAsset();
				break;
		}
	}

	protected virtual bool ValidTargetInVent() => false;

	public override void SetActive(bool visible, RoleBehaviour role)
	{
		if (!visible && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started)
		{
			SetOutline(false);
		}
		base.SetActive(visible, role);
	}

	public virtual void SetOutline(bool active)
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
		}
	}

	/// <summary>
	/// The method used to get the target object.
	/// </summary>
	/// <returns>The target object or null if it isn't found.</returns>
	public abstract TTarget? GetTarget();

	public virtual bool IsTargetValid(TTarget? target)
	{
		if (target is PlayerControl playerTarget)
		{
			return target != null && (ValidTargetInVent() || !playerTarget.inVent) &&
				   !playerTarget.HasModifier<DisabledModifier>(mod => !mod.CanBeInteractedWith) &&
				   !SpectatorRole.TrackedSpectators.Contains(playerTarget.Data.PlayerName);
		}

		return target != null;
	}

	/// <inheritdoc />
	public override bool CanUse()
	{
		var newTarget = GetTarget();
		if (newTarget != Target)
		{
			SetOutline(false);
		}

		Target = IsTargetValid(newTarget) ? newTarget : null;
		SetOutline(true);

		return base.CanUse() && Target != null;
	}

	/// <inheritdoc />
	public override bool CanClick()
	{
		return base.CanClick() && Target != null;
	}

	/// <summary>
	/// Use this to reset the button's target after used.
	/// </summary>
	public virtual void ResetTarget()
	{
		SetOutline(false);
		Target = null;
	}
}
