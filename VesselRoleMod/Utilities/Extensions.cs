using MiraAPI.Modifiers;
using MiraAPI.Roles;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TownOfUs.Roles;
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

	public static void RpcRemoveSelf<T>(this T modifier) where T : BaseModifier
	{
		modifier.Player.RpcRemoveModifier<T>();
	}

	public static void RemoveExistingModifier<T>(this PlayerControl player, Func<T, bool>? predicate = null) where T : BaseModifier
	{
		if (player.TryGetModifier(out var modifier, predicate))
		{
			modifier.RemoveSelf();
		}
	}

	public static void RpcRemoveExistingModifier<T>(this PlayerControl player, Func<T, bool>? predicate = null) where T : BaseModifier
	{
		if (player.TryGetModifier(out var modifier, predicate))
		{
			modifier.RpcRemoveSelf();
		}
	}
}
