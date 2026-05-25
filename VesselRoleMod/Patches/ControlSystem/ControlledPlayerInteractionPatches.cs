using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Usables;
using MiraAPI.Modifiers;
using System.Collections.Generic;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;
using UnityObject = UnityEngine.Object;

namespace VesselRoleMod.Patches.ControlSystem;

/// <summary>
/// Patches to allow vessel poltergeists to trigger interactions for controlled vessels
/// </summary>
[HarmonyPatch]
public static class ControlledPlayerInteractionPatches
{
	private static List<IUsable>? _cachedInteractables;
	private static List<Vent>? _cachedVents;

	private static float _lastCacheRefresh;
	private const float CacheRefreshInterval = 10f;
	private const float UpdateThrottle = 0.1f;
	private static float _lastUpdateTime;

	/// <summary>
	/// Allow UseButton to work for poltergeist when controlling a vessel
	/// </summary>
	[HarmonyPatch(typeof(UseButton), nameof(UseButton.DoClick))]
	[HarmonyPrefix]
	public static bool UseButtonDoClickPrefix(UseButton __instance)
	{
		var localPlayer = PlayerControl.LocalPlayer;
		if (localPlayer == null)
		{
			return true;
		}

		if (localPlayer.TryGetModifier<VesselPossessedModifier>(out var vesselMod) && vesselMod.Ghost != null)
		{
			var controller = vesselMod.Ghost;
			if (controller != null && controller.HasDied() &&
				VesselControlState.IsFullyControlling(controller.PlayerId))
			{
				return false;
			}
		}

		if (localPlayer.TryGetModifier<PoltergeistModifier>(out var poltergeistMod) && poltergeistMod.Vessel != null)
		{
			var controlled = poltergeistMod.Vessel;
			if (controlled != null && !controlled.HasDied() &&
				VesselControlState.IsControllingActionable(localPlayer.PlayerId) &&
				!__instance.isCoolingDown)
			{
				var (interactable, interactablePos) = FindClosestInteractable(controlled);
				if (interactable != null)
				{
					VesselRole.RpcGhostTriggerInteraction(localPlayer, controlled, interactablePos);
					return false;
				}
			}
		}

		return true;
	}

	[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanPet))]
	[HarmonyPrefix]
	public static bool PetButtonCanPetPrefix(PlayerControl __instance, ref bool __result)
	{
		if (LobbyBehaviour.Instance)
		{
			return true;
		}

		if (__instance.TryGetModifier<VesselPossessedModifier>(out var vesselMod) && vesselMod.Ghost != null)
		{
			var controller = vesselMod.Ghost;
			if (controller != null && controller.HasDied() &&
				VesselControlState.IsFullyControlling(controller.PlayerId))
			{
				__result = false;
				return false;
			}
		}

		if (__instance.TryGetModifier<PoltergeistModifier>(out var poltergeistMod) && poltergeistMod.Vessel != null)
		{
			var controlled = poltergeistMod.Vessel;
			if (controlled != null && !controlled.HasDied() &&
				VesselControlState.IsControllingActionable(controlled.PlayerId))
			{
				__result = false;
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Allow UseButton to show as usable when puppeteer/parasite can interact with something
	/// This runs after SetTarget to override the target with the controlled player's interactables
	/// </summary>
	[HarmonyPatch(typeof(UseButton), nameof(UseButton.SetTarget))]
	[HarmonyPriority(Priority.Last)]
	[HarmonyPostfix]
	public static void UseButtonSetTargetPostfix(UseButton __instance)
	{
		UpdateUseButtonTarget(__instance);
	}

	/// <summary>
	/// Refresh cache when ShipStatus loads (new map/game start)
	/// </summary>
	[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake))]
	[HarmonyPostfix]
	public static void ShipStatusAwakePostfix()
	{
		_cachedInteractables = null;
		_lastCacheRefresh = 0f;
	}

	/// <summary>
	/// Also patch HudManager Update to continuously check for interactables near controlled player
	/// Throttled to avoid performance issues
	/// </summary>
	[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
	[HarmonyPriority(Priority.Last)]
	[HarmonyPostfix]
	public static void HudManagerUpdatePostfix(HudManager __instance)
	{
		// Throttle updates to avoid stuttering
		var now = Time.time;
		if (now - _lastUpdateTime < UpdateThrottle)
		{
			return;
		}
		_lastUpdateTime = now;

		if (__instance?.UseButton != null)
		{
			UpdateUseButtonTarget(__instance.UseButton);
		}
	}

	private static void UpdateUseButtonTarget(UseButton useButton)
	{
		var localPlayer = PlayerControl.LocalPlayer;
		if (localPlayer == null || useButton == null)
		{
			return;
		}

		if (localPlayer.TryGetModifier<VesselPossessedModifier>(out var possessedMod) && possessedMod.Ghost != null)
		{
			var controller = possessedMod.Ghost;
			if (controller != null && controller.HasDied() &&
				VesselControlState.IsFullyControlling(controller.PlayerId))
			{
				ClearInteractableOutlines();
				useButton.currentTarget = null;
				useButton.SetDisabled();
			}
			return;
		}

		var isControlling = false;
		PlayerControl? controlledPlayer = null;

		if (localPlayer.TryGetModifier<PoltergeistModifier>(out var poltergeistMod) && poltergeistMod.Vessel != null)
		{
			var controlled = poltergeistMod.Vessel;
			if (controlled != null && !controlled.HasDied() &&
				VesselControlState.IsControllingActionable(localPlayer.PlayerId))
			{
				isControlling = true;
				controlledPlayer = controlled;
			}
		}

		if (!isControlling || controlledPlayer == null)
		{
			return;
		}

		var (usable, _) = FindClosestInteractable(controlledPlayer, setOutlines: true);
		useButton.currentTarget = usable;
		if (usable != null)
		{
			useButton.SetEnabled();
			if (usable.TryCast<IUsableCoolDown>() is { } usableCoolDown)
			{
				useButton.SetCoolDown(usableCoolDown.CoolDown, usableCoolDown.MaxCoolDown);
			}
			else
			{
				useButton.ResetCoolDown();
			}
			useButton.SetCooldownFill(usable.PercentCool);
		}
		else
		{
			useButton.SetDisabled();
			useButton.ResetCoolDown();
		}
	}

	public static void ClearInteractableOutlines()
	{
		var cachedInteractables = GetCachedInteractables();
		foreach (var usable in cachedInteractables)
		{
			usable.SetOutline(false, false);
		}
	}

	/// <summary>
	/// Find the closest interactable object near a player
	/// Uses cached interactables list to avoid expensive FindObjectsOfType every call
	/// </summary>
	public static (IUsable? interactable, Vector2 position) FindClosestInteractable(
		PlayerControl player,
		Vector2? position = null,
		bool setOutlines = false
		)
	{
		if (player == null || player.Collider == null)
		{
			return (null, Vector2.zero);
		}

		var cachedInteractables = GetCachedInteractables();

		if (cachedInteractables == null || cachedInteractables.Count == 0)
		{
			return (null, Vector2.zero);
		}

		var closestDistance = float.MaxValue;
		IUsable? closestInteractable = null;
		Vector2 closestPosition = Vector2.zero;
		Vector2 usePosition = position ?? (Vector2)player.transform.position;

		foreach (var usable in cachedInteractables)
		{
			if (usable == null)
			{
				continue;
			}

			var obj = usable.TryCast<MonoBehaviour>();
			if (obj == null)
			{
				continue;
			}

			var objPos = (Vector2)obj.transform.position;
			var distance = Vector2.Distance(usePosition, objPos);

			usable.CanUse(player.Data, out bool canUse, out bool couldUse);		

			if (setOutlines)
			{
				usable.SetOutline(couldUse && distance <= player.MaxReportDistance, false);
			}

			if (!canUse || distance > player.MaxReportDistance || distance > usable.UsableDistance)
			{
				continue;
			}

			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestInteractable = usable;
				closestPosition = objPos;
			}
		}

		if (setOutlines)
		{
			closestInteractable?.SetOutline(true, true);
		}

		return (closestInteractable, closestPosition);
	}

	/// <summary>
	/// Find the closest interactable object near a player
	/// Uses cached interactables list to avoid expensive FindObjectsOfType every call
	/// </summary>
	public static (Vent? interactable, Vector2 position) FindClosestVent(
		PlayerControl player,
		bool toVent,
		Color? color = null,
		bool setOutlines = false
		)
	{
		if (player == null || player.Collider == null)
		{
			return (null, Vector2.zero);
		}

		var cachedVents = GetCachedVents();

		if (cachedVents == null || cachedVents.Count == 0)
		{
			return (null, Vector2.zero);
		}

		var closestDistance = float.MaxValue;
		Vent? closestVent = null;
		Vector2 closestPosition = Vector2.zero;
		Vector2 usePosition = (Vector2)player.transform.position;
		Color useColor = color ?? PlayerControl.LocalPlayer.Data.Role.TeamColor;

		foreach (var vent in cachedVents)
		{
			if (vent == null)
			{
				continue;
			}

			var ventPos = (Vector2)vent.transform.position;
			var distance = Vector2.Distance(usePosition, ventPos);

			vent.CanUse(player, toVent, out bool canUse, out bool couldUse);

			if (setOutlines)
			{
				vent.SetOutline(couldUse && distance <= player.MaxReportDistance, false, useColor);
			}

			if (!canUse || distance > player.MaxReportDistance || distance > vent.UsableDistance)
			{
				continue;
			}

			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestVent = vent;
				closestPosition = ventPos;
			}
		}

		if (setOutlines)
		{
			closestVent?.SetOutline(true, true, useColor);
		}

		return (closestVent, closestPosition);
	}

	/// <summary>
	/// Public accessor for cached interactables (used by VesselRole RPC handler)
	/// </summary>
	public static List<IUsable> GetCachedInteractables()
	{
		if (_cachedInteractables == null || Time.time - _lastCacheRefresh > CacheRefreshInterval)
		{
			GetInteractablesList(out _cachedInteractables, out _cachedVents);
		}
		return _cachedInteractables!;
	}

	/// <summary>
	/// Public accessor for cached vents (used by VesselRole RPC handler)
	/// </summary>
	public static List<Vent> GetCachedVents()
	{
		if (_cachedVents == null || Time.time - _lastCacheRefresh > CacheRefreshInterval)
		{
			GetInteractablesList(out _cachedInteractables, out _cachedVents);
		}
		return _cachedVents!;
	}

	public static void GetInteractablesList(out List<IUsable> interactables, out List<Vent> vents)
	{
		interactables = new List<IUsable>();
		vents = new List<Vent>();
		var allUsables = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
		foreach (var obj in allUsables)
		{
			if (obj.TryCast<IUsable>() is { } usable)
			{
				if (usable.TryCast<Vent>() is { } vent)
				{
					vents.Add(vent);
				}
				else
				{
					interactables.Add(usable);
				}
			}
		}
	}

	public static float CanUse(this Vent vent, PlayerControl player, bool toVent, out bool canUse, out bool couldUse)
	{
		var canReach = vent.InRange(out float num);
		var beyondReach = num > player.MaxReportDistance;

		if (!beyondReach)
		{
			var @event = new PlayerCanUseEvent(vent.Cast<IUsable>());
			MiraEventManager.InvokeEvent(@event);

			beyondReach = @event.IsCancelled;
		}

		if (beyondReach)
		{
			canUse = false;
			couldUse = false;
			return float.MaxValue;
		}

		couldUse = !toVent ||
			Vent.currentVent == vent ||
			(player.CanMove || player.inVent);

		if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out ISystemType systemType))
		{
			var ventilationSystem = systemType.TryCast<VentilationSystem>();
			if (ventilationSystem != null && ventilationSystem.IsVentCurrentlyBeingCleaned(vent.Id))
			{
				couldUse = false;
			}
		}
		canUse = couldUse;
		if (canUse)
		{
			canUse &= canReach;
		}

		return num;
	}

	public static bool InRange(this Vent vent, out float num)
	{
		var local = PlayerControl.LocalPlayer;
		Vector3 center = local.Collider.bounds.center;
		Vector3 position = vent.transform.position;
		num = Vector2.Distance(center, position);
		return (num <= vent.UsableDistance &&
						 !PhysicsHelpers.AnythingBetween(local.Collider, center, position, Constants.ShipOnlyMask,
							 false));
	}
}
