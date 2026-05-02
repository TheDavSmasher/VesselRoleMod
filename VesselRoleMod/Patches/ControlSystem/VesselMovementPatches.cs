using HarmonyLib;
using MiraAPI.Modifiers;
using Reactor.Networking.Rpc;
using System.Collections.Generic;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Networking;

namespace VesselRoleMod.Patches.ControlSystem;

[HarmonyPatch]
public static class VesselMovementPatches
{
	private static Vector2 GetNormalDirection() => AdvancedMovementUtilities.GetRegularDirection();

	private const float MovementChangeEpsilonSqr = 0.0001f * 0.0001f;
	private const float MovementKeepAliveSeconds = 0.03f;

	private static readonly Dictionary<byte, Vector2> _lastSentDir = new();
	private static readonly Dictionary<byte, Vector2> _lastSentPos = new();
	private static readonly Dictionary<byte, Vector2> _lastSentVel = new();
	private static readonly Dictionary<byte, float> _lastSentAt = new();

	private static readonly Dictionary<byte, Vector2> _localDesiredDir = new();

	private static void SendPlayerInputIfNeeded(byte playerId, bool controlled, Vector2 dir, Vector2 position, Vector2 velocity)
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		var now = Time.time;
		var shouldSend = true;

		if (_lastSentDir.TryGetValue(playerId, out var lastDir) &&
			_lastSentPos.TryGetValue(playerId, out var lastPos) &&
			_lastSentVel.TryGetValue(playerId, out var lastVel) &&
			_lastSentAt.TryGetValue(playerId, out var lastAt))
		{
			var dirChanged = (dir - lastDir).sqrMagnitude > MovementChangeEpsilonSqr;
			var posChanged = (position - lastPos).sqrMagnitude > MovementChangeEpsilonSqr;
			var velChanged = (velocity - lastVel).sqrMagnitude > MovementChangeEpsilonSqr;
			var keepAliveDue = (now - lastAt) >= MovementKeepAliveSeconds;
			shouldSend = dirChanged || posChanged || velChanged || keepAliveDue;
		}

		if (!shouldSend)
		{
			return;
		}

		_lastSentDir[playerId] = dir;
		_lastSentPos[playerId] = position;
		_lastSentVel[playerId] = velocity;
		_lastSentAt[playerId] = now;

		Rpc<VesselInputUnreliableRpc>.Instance.Send(
			PlayerControl.LocalPlayer,
			new VesselInputPacket(playerId, controlled, dir, position, velocity));
	}

	[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
	[HarmonyPrefix]
	public static bool PlayerPhysicsFixedUpdatePrefix(PlayerPhysics __instance)
	{
		var player = __instance.myPlayer;
		if (player == null || player.Data == null)
		{
			return true;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			if (player.HasModifier<VesselPossessedModifier>() && player.AmOwner)
			{
				return true;
			}
			if (player == PlayerControl.LocalPlayer)
			{
				return true;
			}
		}

		// player is local & ghost possessor
		if (player == PlayerControl.LocalPlayer &&
			PlayerControl.LocalPlayer != null &&
			PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>() is var ghost &&
			ghost?.Vessel != null)
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			var vessel = ghost.Vessel;

			if (vessel == null || vessel.Data == null || vessel.HasDied() || vessel.Data.Disconnected)
			{
				return true;
			}

			var shouldMove = Minigame.Instance == null && !player.inVent && !player.inMovingPlat && !player.onLadder && !player.walkingToVent;

			var vesselId = vessel.PlayerId;
			var vesselInAnim = vessel.IsInTargetingAnimState() ||
							   vessel.inVent ||
							   vessel.inMovingPlat ||
							   vessel.onLadder ||
							   vessel.walkingToVent;

			var dir = vesselInAnim ? Vector2.zero : GetNormalDirection();
			_localDesiredDir[vesselId] = dir;

			if (vessel.MyPhysics != null)
			{
				if (dir == Vector2.zero)
				{
					var cachedDir = _localDesiredDir.TryGetValue(vesselId, out var cached) ? cached : Vector2.zero;
					if (cachedDir != Vector2.zero)
					{
						AdvancedMovementUtilities.ApplyControlledMovement(vessel.MyPhysics, cachedDir);
					}
					else
					{
						AdvancedMovementUtilities.ApplyControlledMovement(vessel.MyPhysics, Vector2.zero, stopIfZero: true);
					}
				}
				else
				{
					AdvancedMovementUtilities.ApplyControlledMovement(vessel.MyPhysics, dir, stopIfZero: true);
				}
			}

			var vesselPos = vessel.MyPhysics?.body != null
				? vessel.MyPhysics.body.position
				: (Vector2)vessel.transform.position;
			var vesselVel = vessel.MyPhysics?.body != null
				? vessel.MyPhysics.body.velocity
				: Vector2.zero;

			SendPlayerInputIfNeeded(vesselId, true, dir, vesselPos, vesselVel);

			if (!shouldMove)
			{
				return true;
			}

			var ghostDir = GetNormalDirection();
			AdvancedMovementUtilities.ApplyControlledMovement(__instance, ghostDir, stopIfZero: true);
			return false;
		}

		// player is player vessel
		if (VesselControlState.IsControlled(player.PlayerId, out _))
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			if (player.onLadder || player.inMovingPlat)
			{
				VesselControlState.ClearMovementState(player.PlayerId);
				return true;
			}

			if (player.IsInTargetingAnimState() || player.inVent || player.walkingToVent)
			{
				return true;
			}

			var dir = VesselControlState.GetForcedDirection(player.PlayerId);
			var pos = VesselControlState.GetForcedPosition(player.PlayerId);
			var vel = VesselControlState.GetForcedVelocity(player.PlayerId);

			if (dir == Vector2.zero)
			{
				var cachedDir = _localDesiredDir.TryGetValue(player.PlayerId, out var cached) ? cached : Vector2.zero;
				if (cachedDir != Vector2.zero)
				{
					AdvancedMovementUtilities.ApplyControlledMovement(__instance, cachedDir);
				}
				else
				{
					AdvancedMovementUtilities.ApplyControlledMovement(__instance, Vector2.zero, stopIfZero: true);
				}
			}
			else
			{
				AdvancedMovementUtilities.ApplyControlledMovement(__instance, dir, stopIfZero: true);
			}

			if (pos != Vector2.zero)
			{
				var currentPos = __instance.body != null ? __instance.body.position : (Vector2)__instance.myPlayer.transform.position;
				var delta = pos - currentPos;
				if (delta.magnitude > 0.5f)
				{
					if (__instance.body != null)
					{
						__instance.body.position = pos;
					}
					__instance.myPlayer.transform.position = pos;
				}
			}

			if (__instance.body != null && vel != Vector2.zero)
			{
				__instance.body.velocity = vel;
			}

			return false;
		}

		// player is vessel dummy
		if (player.HasModifier<VesselPossessedModifier>() && player.GetComponent<DummyBehaviour>() != null)
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			if (player.onLadder || player.inMovingPlat)
			{
				VesselControlState.ClearMovementState(player.PlayerId);
				return true;
			}

			if (player.IsInTargetingAnimState() || player.inVent || player.walkingToVent)
			{
				return true;
			}

			var dir = VesselControlState.GetForcedDirection(player.PlayerId);
			var pos = VesselControlState.GetForcedPosition(player.PlayerId);
			var vel = VesselControlState.GetForcedVelocity(player.PlayerId);

			if (dir == Vector2.zero)
			{
				AdvancedMovementUtilities.ApplyControlledMovement(__instance, Vector2.zero, stopIfZero: true);
			}
			else
			{
				AdvancedMovementUtilities.ApplyControlledMovement(__instance, dir, stopIfZero: true);
			}

			if (pos != Vector2.zero)
			{
				var currentPos = __instance.body != null ? __instance.body.position : (Vector2)__instance.myPlayer.transform.position;
				var delta = pos - currentPos;
				if (delta.magnitude > 0.5f)
				{
					if (__instance.body != null)
					{
						__instance.body.position = pos;
					}
					__instance.myPlayer.transform.position = pos;
				}
			}

			if (__instance.body != null && vel != Vector2.zero)
			{
				__instance.body.velocity = vel;
			}

			return false;
		}

		return true;
	}

	[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.SetNormalizedVelocity))]
	[HarmonyPrefix]
	public static bool SetNormalizedVelocityPrefix(PlayerPhysics __instance, ref Vector2 direction)
	{
		var player = __instance.myPlayer;
		if (player == null ||
			!player.HasModifier<VesselPossessedModifier>())
		{
			return true;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return true;
		}

		if (player.AmOwner && VesselControlState.IsControlled(player.PlayerId, out var controllerId))
		{
			if (VesselControlState.CanShareControl)
			{
				direction = VesselControlState.GetDirection(controllerId, player.PlayerId);
			}
			else if (VesselControlState.HasControlOver(controllerId, player.PlayerId))
			{
				direction = VesselControlState.GetForcedDirection(player.PlayerId);
			}
		}

		return true;
	}

	[HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.FixedUpdate))]
	[HarmonyPrefix]
	public static bool CustomNetworkTransformFixedUpdatePrefix(CustomNetworkTransform __instance)
	{
		if (__instance.isPaused || !__instance.myPlayer)
		{
			return true;
		}

		var player = __instance.myPlayer;
		if (!VesselControlState.IsUsingState(player.PlayerId, out byte withId) ||
			VesselControlState.HasControlOver(player.PlayerId, withId))
		{
			return true;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return true;
		}

		if (player.IsInTargetingAnimState() || player.inVent || player.inMovingPlat || player.onLadder || player.walkingToVent)
		{
			return true;
		}

		return false;
	}
}
