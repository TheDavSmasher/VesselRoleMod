using MiraAPI.Modifiers;
using MiraAPI.Roles;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TownOfUs.Roles;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace VesselRoleMod.Utilities;

public static class Extensions
{
	public static bool HasModifierOfType<T>(this PlayerControl player, Func<T, bool>? predicate = null)
	{
		return player.GetModifierComponent().ActiveModifiers.OfType<T>().Any(predicate ?? (_ => true));
	}

	public static T? GetModifierOfType<T>(this PlayerControl player, Func<T, bool>? predicate = null)
	{
		return player.GetModifierComponent().ActiveModifiers.OfType<T>().Where(predicate ?? (_ => true)).FirstOrDefault();
	}

	public static bool TryGetModifierOfType<T>(this PlayerControl player, [NotNullWhen(true)] out T? modifier, Func<T, bool>? predicate = null)
	{
		modifier = GetModifierOfType(player, predicate);
		return modifier != null;
	}

	public static bool HasKillingAbility(this PlayerControl player)
	{
		return player.Data.Role.HasKillingAbility();
	}

	public static bool HasKillingAbility(this RoleBehaviour role)
	{
		var alignment = role.GetRoleAlignment();

		return role.IsImpostor() ||
			   (alignment == RoleAlignment.NeutralKilling) ||
			   (alignment == RoleAlignment.CrewmateKilling);
	}

	public static bool HasVentingAbility(this RoleBehaviour role)
	{
		return role.CanVent ||
			   role is ICustomRole custom && custom.Configuration.CanUseVent ||
			   role is EngineerRole ||
			   role is EngineerTouRole ||
			   role.Player.HasModifier<BaseModifier>(x => x.CanVent() == true);
	}

	public static GameObject FindChildObject(this Transform transform, string n)
	{
		return transform.FindChild(n).gameObject;
	}

	public static T FindChildComponent<T>(this Transform transform, string n)
	{
		return transform.FindChildObject(n).GetComponent<T>();
	}

	public const float DirectionDeadzone = 0.125f;

	public static Vector2 ApplyDeadzone(this Vector2 v)
	{
		return v.sqrMagnitude < DirectionDeadzone * DirectionDeadzone ? Vector2.zero : v;
	}

	public static void RemoveSelf(this BaseModifier modifier)
	{
		modifier.ModifierComponent?.RemoveModifier(modifier);
	}

	public static void RemoveExistingModifier<T>(this PlayerControl player, Func<T, bool>? predicate = null) where T : BaseModifier
	{
		if (player.TryGetModifier(out var modifier, predicate))
		{
			modifier.RemoveSelf();
		}
	}

	public static void ClearVentState(this PlayerPhysics physics, bool exitAlways = false)
	{
		if (!physics.AmOwner)
		{
			return;
		}
		if (physics.myPlayer.inVent)
		{
			physics.RpcExitVent(Vent.currentVent.Id);
		}
		if (physics.myPlayer.inVent || exitAlways)
		{
			/// Exit All Vents
			ConsoleJoystick.SetMode_Gameplay();
			Vent.currentVent = null;
			physics.ResetMoveState(false);
			physics.myPlayer.moveable = true;
			Vent[] allVents = ShipStatus.Instance.AllVents;
			for (int i = 0; i < allVents.Length; i++)
			{
				allVents[i].SetButtons(enabled: false);
			}
		}
	}
}
