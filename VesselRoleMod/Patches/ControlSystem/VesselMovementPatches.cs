using HarmonyLib;
using MiraAPI.Modifiers;
using Reactor.Networking.Rpc;
using System;
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
	private static readonly Dictionary<byte, Vector2> _lastSentForceDir = new();
	private static readonly Dictionary<byte, Vector2> _lastSentSelfDir = new();

	private static readonly Dictionary<byte, Vector2> _lastSentForcePos = new();
	private static readonly Dictionary<byte, Vector2> _lastSentSelfPos = new();

	private static readonly Dictionary<byte, Vector2> _lastSentForceVel = new();
	private static readonly Dictionary<byte, Vector2> _lastSentSelfVel = new();

	private static readonly Dictionary<byte, float> _lastSentForceAt = new();
	private static readonly Dictionary<byte, float> _lastSentSelfAt = new();

	private static readonly Dictionary<byte, Vector2> _localDesiredForceDir = new();
	private static readonly Dictionary<byte, Vector2> _localDesiredSelfDir = new();

	private static void SendControlledInputIfNeeded(byte controlledId, Vector2 dir, Vector2 position, Vector2 velocity)
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		var now = Time.time;
		var shouldSend = true;

		if (_lastSentForceDir.TryGetValue(controlledId, out var lastDir) &&
			_lastSentForcePos.TryGetValue(controlledId, out var lastPos) &&
			_lastSentForceVel.TryGetValue(controlledId, out var lastVel) &&
			_lastSentForceAt.TryGetValue(controlledId, out var lastAt))
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

		_lastSentForceDir[controlledId] = dir;
		_lastSentForcePos[controlledId] = position;
		_lastSentForceVel[controlledId] = velocity;
		_lastSentForceAt[controlledId] = now;

		Rpc<VesselInputUnreliableRpc>.Instance.Send(
			PlayerControl.LocalPlayer,
			new VesselInputPacket(controlledId, dir, position, velocity));
	}

	private static void SendSelfInputIfNeeded(byte controllerId, Vector2 dir, Vector2 position, Vector2 velocity)
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		var now = Time.time;
		var shouldSend = true;

		if (_lastSentSelfDir.TryGetValue(controllerId, out var lastDir) &&
			_lastSentSelfPos.TryGetValue(controllerId, out var lastPos) &&
			_lastSentSelfVel.TryGetValue(controllerId, out var lastVel) &&
			_lastSentSelfAt.TryGetValue(controllerId, out var lastAt))
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

		_lastSentForceDir[controllerId] = dir;
		_lastSentForcePos[controllerId] = position;
		_lastSentForceVel[controllerId] = velocity;
		_lastSentForceAt[controllerId] = now;

		Rpc<PoltergeistInputUnreliableRpc>.Instance.Send(
			PlayerControl.LocalPlayer,
			new PoltergeistInputPacket(controllerId, dir, position, velocity));
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
			//var canShareControl = OptionGroupSingleton<VesselOptions>.Instance.CanShareControl;

			var vesselId = vessel.PlayerId;
			var vesselInAnim = vessel.IsInTargetingAnimState() ||
							   vessel.inVent ||
							   vessel.inMovingPlat ||
							   vessel.onLadder ||
							   vessel.walkingToVent;

			var dir = vesselInAnim ? Vector2.zero : GetNormalDirection();
			_localDesiredForceDir[vesselId] = dir;

			if (vessel.MyPhysics != null)
			{
				if (dir == Vector2.zero)
				{
					var cachedDir = _localDesiredForceDir.TryGetValue(vesselId, out var cached) ? cached : Vector2.zero;
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

			SendControlledInputIfNeeded(vesselId, dir, vesselPos, vesselVel);

			if (!shouldMove)
			{
				return true;
			}

			var ghostDir = GetNormalDirection();
			AdvancedMovementUtilities.ApplyControlledMovement(__instance, ghostDir, stopIfZero: true);
			return false;
		}

		if (VesselControlState.IsControlled(player.PlayerId, out _))
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			if (player.onLadder || player.inMovingPlat)
			{
				VesselControlState.ClearForcedMovementState(player.PlayerId);
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
				var cachedDir = _localDesiredForceDir.TryGetValue(player.PlayerId, out var cached) ? cached : Vector2.zero;
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

		if (player.HasModifier<VesselPossessedModifier>() && player.GetComponent<DummyBehaviour>() != null)
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			if (player.onLadder || player.inMovingPlat)
			{
				VesselControlState.ClearForcedMovementState(player.PlayerId);
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
			!player.HasModifier<VesselPossessedModifier>() || 
			!player.HasModifier<PoltergeistModifier>())
		{
			return true;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return true;
		}

		if (player.AmOwner)
		{
			if (VesselControlState.IsControlled(player.PlayerId, out var controllerId))
			{
				direction = CombineVesselStateVectors(controllerId, player.PlayerId,
					VesselControlState.GetForcedDirection, VesselControlState.GetSelfDirection);
			}
			if (VesselControlState.IsControlling(player.PlayerId, out var controlledId))
			{
				direction = CombineVesselStateVectors(player.PlayerId, controlledId,
					VesselControlState.GetForcedDirection, VesselControlState.GetSelfDirection);
			}
		}

		return true;
	}

	private static Vector2 CombineVesselStateVectors(
		byte controllerId,
		byte controlledId,
		Func<byte, Vector2> getForcedVector,
		Func<byte, Vector2> getSelfVector)
	{
		return CombineMovementVectors(
			getForcedVector(controlledId),
			getSelfVector(controllerId)
			);
	}

	private static Vector2 CombineMovementVectors(Vector2 v1, Vector2 v2)
	{
		return new Vector2(
			CombineMovementFloats(v1.x, v2.x),
			CombineMovementFloats(v1.y, v2.y)
			);
	}

	private static float CombineMovementFloats(float f1, float f2)
	{
		var fr = f1 * f2;
		if (fr > 0)
		{
			return Math.Max(f1, f2);
		}
		if (fr < 0)
		{
			return f1 + f2;
		}
		else
		{
			return f1 != 0f ? f1 : f2;
		}
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
		if (!VesselControlState.IsControlled(player.PlayerId, out _) ||
			!VesselControlState.IsControlling(player.PlayerId, out _))
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
