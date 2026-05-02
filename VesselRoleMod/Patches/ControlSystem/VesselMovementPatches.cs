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
using VesselRoleMod.Roles.Crewmate;

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

	private static bool CollectLocalVesselInput(PlayerControl vessel, bool controlled)
	{
		if (TimeLordRewindSystem.IsRewinding)
		{
			return true;
		}

		if (vessel == null || vessel.Data == null || vessel.HasDied() || vessel.Data.Disconnected)
		{
			return true;
		}

		var vesselId = vessel.PlayerId;
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

		SendPlayerInputIfNeeded(vesselId, controlled, dir, vesselPos, vesselVel);
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
			PlayerControl.LocalPlayer != null)
		{
			if (PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>() is var ghost &&
				ghost?.Vessel != null &&
				CollectLocalVesselInput(ghost.Vessel, true))
			{
				return true;
			}

			if (PlayerControl.LocalPlayer.Data.Role is VesselRole vesselRole &&
				vesselRole.Ghost != null &&
				CollectLocalVesselInput(player, false))
			{
				return true;
			}
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
				VesselControlState.ClearMovementState(player.PlayerId, ghostId);
				return true;
			}

			if (player.IsInTargetingAnimState() || player.inVent || player.walkingToVent)
			{
				return true;
			}

			Vector2 dir, pos, vel;
			if (VesselControlState.CanShareControl)
			{
				dir = VesselControlState.GetDirection(ghostId, player.PlayerId);
				pos = VesselControlState.GetPosition(ghostId, player.PlayerId);
				vel = VesselControlState.GetVelocity(ghostId, player.PlayerId);
			}
			else if (VesselControlState.HasControlOver(ghostId, player.PlayerId))
			{
				dir = VesselControlState.GetForcedDirection(player.PlayerId);
				pos = VesselControlState.GetForcedPosition(player.PlayerId);
				vel = VesselControlState.GetForcedVelocity(player.PlayerId);
			}
			else
			{
				dir = VesselControlState.GetSelfDirection(ghostId);
				pos = VesselControlState.GetSelfPosition(ghostId);
				vel = VesselControlState.GetSelfVelocity(ghostId);
			}

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

			Vector2 dir, pos, vel;
			if (VesselControlState.CanShareControl || VesselControlState.HasControlOver(ghostId, player.PlayerId))
			{
				dir = VesselControlState.GetForcedDirection(player.PlayerId);
				pos = VesselControlState.GetForcedPosition(player.PlayerId);
				vel = VesselControlState.GetForcedVelocity(player.PlayerId);
			}
			else
			{
				dir = Vector2.zero;
				pos = Vector2.zero;
				vel = Vector2.zero;
			}

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
