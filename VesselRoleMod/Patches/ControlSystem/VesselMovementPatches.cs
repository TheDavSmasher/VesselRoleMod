using HarmonyLib;
using MiraAPI.Modifiers;
using Reactor.Networking.Rpc;
using System.Collections.Generic;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modifiers.Crewmate;
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

	private static void SendVesselInputIfNeeded(byte targetId, bool fromVessel, Vector2 dir, Vector2 position, Vector2 velocity)
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		var now = Time.time;
		var shouldSend = true;

		if (_lastSentDir.TryGetValue(targetId, out var lastDir) &&
			_lastSentPos.TryGetValue(targetId, out var lastPos) &&
			_lastSentVel.TryGetValue(targetId, out var lastVel) &&
			_lastSentAt.TryGetValue(targetId, out var lastAt))
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

		_lastSentDir[targetId] = dir;
		_lastSentPos[targetId] = position;
		_lastSentVel[targetId] = velocity;
		_lastSentAt[targetId] = now;

		Rpc<VesselInputUnreliableRpc>.Instance.Send(
			PlayerControl.LocalPlayer,
			new VesselInputPacket(targetId, fromVessel, dir, position, velocity));
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


		if (player.AmOwner &&
			PlayerControl.LocalPlayer &&
			PlayerControl.LocalPlayer.GetModifierOfType<IVesselModifier>() is { } mod &&
			mod.Vessel != null &&
			VesselControlState.HasControl(PlayerControl.LocalPlayer.PlayerId))
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			var vessel = mod.Vessel;
			if (vessel.Data == null || vessel.HasDied() || vessel.Data.Disconnected)
			{
				return true;
			}

			var vesselInAnim = vessel.IsInTargetingAnimState() || vessel.inVent;

			var dir = GetNormalDirection();

			var vesselVel = vesselInAnim ? Vector2.zero : vessel.MyPhysics.TrueSpeed * dir;
			var vesselPos = vessel.MyPhysics?.body.position ?? vessel.transform.position;

			SendVesselInputIfNeeded(mod.Target.PlayerId, vessel.AmOwner, dir, vesselPos, vesselVel);
		}

		// player is player ghost
		if (VesselControlState.IsControlling(player.PlayerId, out var vesselId))
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			Vector2 dir = VesselControlState.GetDirection(player.PlayerId, vesselId);

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

			if (player.IsInTargetingAnimState() || player.inVent)
			{
				return true;
			}

			Vector2 dir = VesselControlState.GetDirection(ghostId, player.PlayerId);

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

			if (player.IsInTargetingAnimState() || player.inVent)
			{
				return true;
			}

			Vector2 dir = VesselControlState.GetDirection(player.PlayerId);

			ApplyAllDataTo(__instance, player.PlayerId, dir);
			return false;
		}

		return true;
	}

	private static void ApplyAllDataTo(PlayerPhysics __instance, byte vesselId, Vector2 dir)
	{
		Vector2 pos = VesselControlState.GetPosition(vesselId);
		Vector2 vel = VesselControlState.GetVelocity(vesselId);

		AdvancedMovementUtilities.ApplyControlledMovement(__instance, dir);

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
			var currentVel = __instance.body.velocity;
			var delta = vel - currentVel;
			if (delta.magnitude > 0.5f)
			{
				__instance.body.velocity = vel;
			}
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

		if (player.AmOwner && VesselControlState.IsUsingState(player.PlayerId, out _))
		{
			direction = VesselControlState.GetDirection(player.PlayerId);
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
		if (!VesselControlState.IsUsingStateControl(player.PlayerId, out var hasControl))
		{
			return true;
		}

		if (hasControl)
		{
			return true;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return true;
		}

		if (player.IsInTargetingAnimState() || player.inVent)
		{
			return true;
		}

		return false;
	}
}
