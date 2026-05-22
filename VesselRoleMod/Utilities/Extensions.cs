using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using System;
using System.Linq;
using TownOfUs.Buttons;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace VesselRoleMod.Utilities;

public static class Extensions
{
	public static bool HasModifierOfType<T>(this PlayerControl player, Func<T, bool>? predicate = null)
	{
		return player.GetModifierComponent() is { } comp && comp.ActiveModifiers.OfType<T>().Any(predicate ?? (_ => true));
	}

	public static T? GetModifierOfType<T>(this PlayerControl player, Func<T, bool>? predicate = null)
	{
		return player.GetModifierComponent() is { } comp &&
			comp.ActiveModifiers.OfType<T>().FirstOrDefault(predicate ?? (_ => true)) is T res ? res : default;
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
			   CustomButtonSingleton<FakeVentButton>.Instance.Show ||
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
}
