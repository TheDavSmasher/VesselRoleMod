using HarmonyLib;
using MiraAPI.Modifiers;
using System.Collections.Generic;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Patches.ControlSystem.Interactions;

/// <summary>
/// Patches to allow vessel poltergeists to trigger interactions for controlled vessels
/// </summary>
[HarmonyPatch]
public static class ControlledPlayerInteractionPatches
{
	private static List<IUsable>? _cachedInteractables;
	private static float _lastCacheRefresh;
	private const float CacheRefreshInterval = 10f;

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

		if (localPlayer.TryGetModifier<VesselPossessedModifier>(out var vesselMod))
		{
			var controller = vesselMod.Ghost;
			if (controller != null && controller.HasDied() &&
				VesselControlState.IsControlling(controller.PlayerId, out _))
			{
				if (VesselControlState.HasControl(localPlayer.PlayerId) &&
					__instance.currentTarget.TryCast<MonoBehaviour>() is { } obj)
				{
					VesselRole.RpcVesselSetGhostState(controller, localPlayer, obj.transform.position);
					return true;
				}
				return false;
			}
		}

		if (localPlayer.TryGetModifier<PoltergeistModifier>(out var poltergeistMod))
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

		if (__instance.TryGetModifier<VesselPossessedModifier>(out var vesselMod))
		{
			var controller = vesselMod.Ghost;
			if (controller != null && controller.HasDied() &&
				VesselControlState.IsFullyControlling(controller.PlayerId))
			{
				return (__result = false);
			}
		}

		if (__instance.TryGetModifier<PoltergeistModifier>(out var poltergeistMod))
		{
			var controlled = poltergeistMod.Vessel;
			if (controlled != null && !controlled.HasDied() &&
				VesselControlState.IsControllingActionable(controlled.PlayerId))
			{
				return (__result = false);
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

	private static void UpdateUseButtonTarget(UseButton useButton)
	{
		var localPlayer = PlayerControl.LocalPlayer;
		if (localPlayer == null || useButton == null)
		{
			return;
		}

		if (localPlayer.TryGetModifier<VesselPossessedModifier>(out var possessedMod))
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

		if (localPlayer.TryGetModifier<PoltergeistModifier>(out var poltergeistMod))
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
	public static IUsable? FindClosestInteractable(PlayerControl player, Vector2 position)
	{
		return FindClosestInteractable(player, position, false).interactable;
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
	/// Public accessor for cached interactables (used by VesselRole RPC handler)
	/// </summary>
	public static List<IUsable> GetCachedInteractables()
	{
		if (_cachedInteractables == null || Time.time - _lastCacheRefresh > CacheRefreshInterval)
		{
			_cachedInteractables = GetInteractablesList();
			_lastCacheRefresh = Time.time;
		}
		return _cachedInteractables!;
	}

	public static List<IUsable> GetInteractablesList()
	{
		List<IUsable> interactables = [];
		var allUsables = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
		foreach (var obj in allUsables)
		{
			if (obj.TryCast<IUsable>() is { } usable &&
				usable.TryCast<Vent>() == null &&
				!obj.name.Contains("Vent"))
			{
				interactables.Add(usable);
			}
		}
		return interactables;
	}
}
