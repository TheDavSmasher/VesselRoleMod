using HarmonyLib;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Reactor.Networking.Rpc;
using System.Collections.Generic;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Networking;
using VesselRoleMod.Utilities;

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

	private static void SendPlayerStateIfNeeded(byte playerId, Vector2 position, Vector2 velocity)
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		var now = Time.time;
		var shouldSend = true;

		if (_lastSentPos.TryGetValue(playerId, out var lastPos) &&
			_lastSentVel.TryGetValue(playerId, out var lastVel) &&
			_lastSentAt.TryGetValue(playerId, out var lastAt))
		{
			var posChanged = (position - lastPos).sqrMagnitude > MovementChangeEpsilonSqr;
			var velChanged = (velocity - lastVel).sqrMagnitude > MovementChangeEpsilonSqr;
			var keepAliveDue = (now - lastAt) >= MovementKeepAliveSeconds;
			shouldSend = posChanged || velChanged || keepAliveDue;
		}

		if (!shouldSend)
		{
			return;
		}

		_lastSentPos[playerId] = position;
		_lastSentVel[playerId] = velocity;
		_lastSentAt[playerId] = now;

		Rpc<VesselStateUnreliableRpc>.Instance.Send(
			PlayerControl.LocalPlayer,
			new VesselStatePacket(playerId, position, velocity));
	}

	private static void SendPlayerInputIfNeeded(byte playerId, bool controlled, Vector2 dir)
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
			var keepAliveDue = (now - lastAt) >= MovementKeepAliveSeconds;
			shouldSend = dirChanged|| keepAliveDue;
		}

		if (!shouldSend)
		{
			return;
		}

		_lastSentDir[playerId] = dir;
		_lastSentAt[playerId] = now;

		Rpc<VesselInputUnreliableRpc>.Instance.Send(
			PlayerControl.LocalPlayer,
			new VesselInputPacket(playerId, controlled, dir));
	}

	private static bool CollectLocalVesselInput(PlayerControl vessel, byte playerId, bool controlled)
	{
		if (vessel == null || vessel.Data == null || vessel.HasDied() || vessel.Data.Disconnected)
		{
			return true;
		}

		var vesselInAnim = vessel.IsInTargetingAnimState() ||
						   vessel.inVent ||
						   vessel.inMovingPlat ||
						   vessel.onLadder ||
						   vessel.walkingToVent;

		var dir = vesselInAnim ? Vector2.zero : GetNormalDirection();
		
		SendPlayerInputIfNeeded(playerId, controlled, dir);
		return false;
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

		if (player == PlayerControl.LocalPlayer &&
			PlayerControl.LocalPlayer != null &&
			PlayerControl.LocalPlayer.GetModifierOfType<IVesselModifier>() is { } mod1)
		{
			if (TimeLordRewindSystem.IsRewinding && player.AmOwner)
			{
				return true;
			}

			var controlled = mod1 is PoltergeistModifier;
			if (CollectLocalVesselInput(mod1.Vessel, mod1.Target.PlayerId, controlled))
			{
				return true;
			}

			if (controlled)
			{
				var vessel = mod1.Vessel;

				var vesselPos = vessel.MyPhysics?.body != null
					? vessel.MyPhysics.body.position
					: (Vector2)vessel.transform.position;
				var vesselVel = vessel.MyPhysics?.body != null
					? vessel.MyPhysics.body.velocity
					: Vector2.zero;

				SendPlayerStateIfNeeded(vessel.PlayerId, vesselPos, vesselVel);
			}
		}

		// host send position

		// player is player ghost
		if (VesselControlState.IsControlling(player.PlayerId, out var vesselId))
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			if (player.onLadder || player.inMovingPlat)
			{
				VesselControlState.ClearMovementState(vesselId);
				return true;
			}

			if (player.IsInTargetingAnimState() || player.inVent || player.walkingToVent)
			{
				return true;
			}

			Vector2 dir;
			if (VesselControlState.CanShareControl)
			{
				dir = VesselControlState.GetDirection(player.PlayerId, vesselId);
			}
			else if (VesselControlState.HasControlOver(vesselId, player.PlayerId))
			{
				dir = VesselControlState.GetSelfDirection(player.PlayerId);
			}
			else
			{
				dir = VesselControlState.GetForcedDirection(vesselId);
			}

			ApplyAllDataTo(__instance, vesselId, dir);
			return false;
		}

		// player is player vessel
		if (VesselControlState.IsControlled(player.PlayerId, out var ghostId))
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

			Vector2 dir;
			if (VesselControlState.CanShareControl)
			{
				dir = VesselControlState.GetDirection(ghostId, player.PlayerId);
			}
			else if (VesselControlState.HasControlOver(ghostId, player.PlayerId))
			{
				dir = VesselControlState.GetForcedDirection(player.PlayerId);
			}
			else
			{
				dir = VesselControlState.GetSelfDirection(ghostId);
			}

			ApplyAllDataTo(__instance, player.PlayerId, dir);
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

			Vector2 dir;
			if (VesselControlState.CanShareControl || !VesselControlState.HasControl(player.PlayerId))
			{
				dir = VesselControlState.GetForcedDirection(player.PlayerId);
			}
			else
			{
				dir = Vector2.zero;
			}

			ApplyAllDataTo(__instance, player.PlayerId, dir);
			return false;
		}

		return true;
	}

	private static void ApplyAllDataTo(PlayerPhysics __instance, byte vesselId, Vector2 dir)
	{
		if (dir == Vector2.zero)
		{
			AdvancedMovementUtilities.ApplyControlledMovement(__instance, Vector2.zero, stopIfZero: true);
		}
		else
		{
			AdvancedMovementUtilities.ApplyControlledMovement(__instance, dir, stopIfZero: true);
		}

		Vector2 pos = VesselControlState.GetPosition(vesselId);
		Vector2 vel = VesselControlState.GetVelocity(vesselId);

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
	}

	[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.SetNormalizedVelocity))]
	[HarmonyPrefix]
	public static bool SetNormalizedVelocityPrefix(PlayerPhysics __instance, ref Vector2 direction)
	{
		var player = __instance.myPlayer;
		if (player == null || !player.HasModifierOfType<IVesselModifier>())
		{
			return true;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return true;
		}

		if (player.AmOwner && VesselControlState.IsUsingState(player.PlayerId, out var withId))
		{
			byte controlledId, controllerId;
			if (VesselControlState.IsControlled(player.PlayerId, out _))
			{
				controlledId = player.PlayerId;
				controllerId = withId;
			}
			else
			{
				controlledId = withId;
				controllerId = player.PlayerId;
			}

			if (VesselControlState.CanShareControl)
			{
				direction = VesselControlState.GetDirection(controllerId, controlledId);
			}
			else if (VesselControlState.HasControlOver(controllerId, controlledId))
			{
				direction = VesselControlState.GetForcedDirection(controlledId);
			}
			else
			{
				direction = VesselControlState.GetSelfDirection(controllerId);
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
