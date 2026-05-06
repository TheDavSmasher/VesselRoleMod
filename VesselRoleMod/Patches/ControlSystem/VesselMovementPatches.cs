using HarmonyLib;
using MiraAPI.Modifiers;
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

	private static void SendVesselInputIfNeeded(byte vesselId, Vector2 dir, Vector2 position, Vector2 velocity)
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		var now = Time.time;
		var shouldSend = true;

		if (_lastSentDir.TryGetValue(vesselId, out var lastDir) &&
			_lastSentPos.TryGetValue(vesselId, out var lastPos) &&
			_lastSentVel.TryGetValue(vesselId, out var lastVel) &&
			_lastSentAt.TryGetValue(vesselId, out var lastAt))
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

		_lastSentDir[vesselId] = dir;
		_lastSentPos[vesselId] = position;
		_lastSentVel[vesselId] = velocity;
		_lastSentAt[vesselId] = now;

		Rpc<PoltergeistInputUnreliableRpc>.Instance.Send(
			PlayerControl.LocalPlayer,
			new PoltergeistInputPacket(vesselId, dir, position, velocity));
	}

	private static void SendGhostInputIfNeeded(byte ghostId, Vector2 dir)
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		var now = Time.time;
		var shouldSend = true;

		if (_lastSentDir.TryGetValue(ghostId, out var lastDir) &&
			_lastSentAt.TryGetValue(ghostId, out var lastAt))
		{
			var dirChanged = (dir - lastDir).sqrMagnitude > MovementChangeEpsilonSqr;
			var keepAliveDue = (now - lastAt) >= MovementKeepAliveSeconds;
			shouldSend = dirChanged|| keepAliveDue;
		}

		if (!shouldSend)
		{
			return;
		}

		_lastSentDir[ghostId] = dir;
		_lastSentAt[ghostId] = now;

		Rpc<VesselInputUnreliableRpc>.Instance.Send(
			PlayerControl.LocalPlayer,
			new VesselInputPacket(ghostId, dir));
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
			if (player.HasModifierOfType<IVesselModifier>() && player.AmOwner)
			{
				return true;
			}
			if (player == PlayerControl.LocalPlayer)
			{
				return true;
			}
		}


		if (player == PlayerControl.LocalPlayer &&
			PlayerControl.LocalPlayer != null)
		{
			if (PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>() is { } mod &&
				mod.Vessel != null)
			{
				if (TimeLordRewindSystem.IsRewinding)
				{
					return true;
				}

				var vessel = mod.Vessel;

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
				var vesselPos = vessel.MyPhysics?.body != null
					? vessel.MyPhysics.body.position
					: (Vector2)vessel.transform.position;
				var vesselVel = vessel.MyPhysics?.body != null
					? vessel.MyPhysics.body.velocity
					: Vector2.zero;

				SendVesselInputIfNeeded(vessel.PlayerId, dir, vesselPos, vesselVel);
			}
			else if (PlayerControl.LocalPlayer.GetModifier<VesselPossessedModifier>() is { } mod2 &&
				mod2.Ghost != null)
			{
				if (TimeLordRewindSystem.IsRewinding)
				{
					return true;
				}

				var vessel = player;

				if (player == null || player.Data == null || player.HasDied() || player.Data.Disconnected)
				{
					return true;
				}

				var vesselInAnim = vessel.IsInTargetingAnimState() ||
								   vessel.inVent ||
								   vessel.inMovingPlat ||
								   vessel.onLadder ||
								   vessel.walkingToVent;

				var dir = vesselInAnim ? Vector2.zero : GetNormalDirection();
				SendGhostInputIfNeeded(mod2.Ghost.PlayerId, dir);
			}
		}

		// player is player ghost
		if (VesselControlState.IsControlling(player.PlayerId, out var vesselId))
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			Vector2 dir = VesselControlState.GetFinalDirection(player.PlayerId, vesselId);

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

			Vector2 dir = VesselControlState.GetFinalDirection(ghostId, player.PlayerId);

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

			Vector2 dir = VesselControlState.GetFinalDirection(player.PlayerId);

			ApplyAllDataTo(__instance, player.PlayerId, dir);
			return false;
		}

		return true;
	}

	private static void ApplyAllDataTo(PlayerPhysics __instance, byte vesselId, Vector2 dir)
	{
		Vector2 pos = VesselControlState.GetPosition(vesselId);
		Vector2 vel = VesselControlState.GetVelocity(vesselId);

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
		if (!VesselControlState.IsUsingState(player.PlayerId, out _))
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
