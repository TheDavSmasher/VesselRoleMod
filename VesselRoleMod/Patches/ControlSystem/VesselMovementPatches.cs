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
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Patches.ControlSystem;

[HarmonyPatch]
public static class VesselMovementPatches
{
	private static Vector2 GetNormalDirection() => AdvancedMovementUtilities.GetRegularDirection();

	private const float MovementChangeEpsilonSqr = 0.0001f * 0.0001f;
	private const float MovementKeepAliveSeconds = 0.03f;

	private static readonly Dictionary<byte, Vector2> _lastSentDir = [];
	private static readonly Dictionary<byte, Vector2> _lastSentPos = [];
	private static readonly Dictionary<byte, Vector2> _lastSentVel = [];
	private static readonly Dictionary<byte, float> _lastSentDirAt = [];
	private static readonly Dictionary<byte, float> _lastSentStateAt = [];


	private static void SendVesselInputIfNeeded(byte targetId, bool fromVessel, Vector2 dir)
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		var now = Time.time;
		var shouldSend = true;

		if (_lastSentDir.TryGetValue(targetId, out var lastDir) &&
			_lastSentDirAt.TryGetValue(targetId, out var lastAt))
		{
			var dirChanged = (dir - lastDir).sqrMagnitude > MovementChangeEpsilonSqr;
			var keepAliveDue = (now - lastAt) >= MovementKeepAliveSeconds;
			shouldSend = dirChanged || keepAliveDue;
		}

		if (!shouldSend)
		{
			return;
		}

		_lastSentDir[targetId] = dir;
		_lastSentDirAt[targetId] = now;

		Rpc<VesselInputUnreliableRpc>.Instance.Send(
			PlayerControl.LocalPlayer,
			new VesselInputPacket(targetId, fromVessel, dir));
	}

	private static void SendVesselStateIfNeeded(byte vesselId, Vector2 position, bool inAnim, Vector2 velocity)
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		var now = Time.time;
		var shouldSend = true;

		if (_lastSentPos.TryGetValue(vesselId, out var lastPos) &&
			_lastSentVel.TryGetValue(vesselId, out var lastVel) &&
			_lastSentStateAt.TryGetValue(vesselId, out var lastAt))
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

		_lastSentPos[vesselId] = position;
		_lastSentVel[vesselId] = velocity;

		Rpc<VesselStateUnreliableRpc>.Instance.Send(
			PlayerControl.LocalPlayer,
			new VesselStatePacket(vesselId, position, inAnim, velocity));
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
			if (player.HasModifierOfType<IVesselPossessModifier>() && player.AmOwner)
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
			PlayerControl.LocalPlayer.TryGetModifierOfType<IVesselPossessModifier>(out var mod) &&
			mod.Vessel != null)
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			if (VesselControlState.HasControl(PlayerControl.LocalPlayer.PlayerId))
			{
				var vessel = mod.Vessel;
				if (vessel.Data == null || vessel.HasDied() || vessel.Data.Disconnected)
				{
					return true;
				}

				var dir = GetNormalDirection();

				SendVesselInputIfNeeded(mod.Target.PlayerId, vessel.AmOwner, dir);
			}
			if (mod is VesselPossessedModifier && player.Data.Role is VesselRole)
			{
				var vesselInAnim = player.IsInTargetingAnimState() || player.inVent;

				var vesselVel = vesselInAnim && player.MyPhysics != null
					? player.MyPhysics.body.velocity
					: Vector2.zero;
				var vesselPos = player.MyPhysics?.body.position ?? player.transform.position;

				SendVesselStateIfNeeded(player.PlayerId, vesselPos, vesselInAnim, vesselVel);
			}
		}

		// player is player ghost
		if (VesselControlState.IsControlling(player.PlayerId, out var vesselId))
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			ApplyAllDataTo(__instance, vesselId);
			return false;
		}

		// player is player vessel
		if (VesselControlState.IsControlled(player.PlayerId, out var ghostId))
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			if (player.IsInTargetingAnimState() || player.inVent)
			{
				return true;
			}

			ApplyAllDataTo(__instance, player.PlayerId);
			return false;
		}

		// player is vessel dummy
		if (player.HasModifier<VesselPossessedModifier>() && player.GetComponent<DummyBehaviour>() != null)
		{
			if (TimeLordRewindSystem.IsRewinding)
			{
				return true;
			}

			if (player.IsInTargetingAnimState() || player.inVent)
			{
				return true;
			}

			ApplyAllDataTo(__instance, player.PlayerId);
			return false;
		}

		return true;
	}

	private static void ApplyAllDataTo(PlayerPhysics __instance, byte vesselId)
	{
		Vector2 dir = VesselControlState.GetDirection(vesselId);
		Vector2 pos = VesselControlState.GetPosition(vesselId);
		Vector2 vel = VesselControlState.GetVelocity(vesselId);
		bool inAnim = VesselControlState.IsInAnim(vesselId);

		__instance.HandleAnimation(__instance.myPlayer.Data.IsDead);

		if (__instance.body != null)
		{
			if (inAnim)
			{
				__instance.body.velocity = vel;
			}
			else
			{
				__instance.SetNormalizedVelocity(dir);
				// __instance.body.velocity = dir * __instance.TrueSpeed;
			}
		}

		if (pos != Vector2.zero)
		{
			var currentPos = __instance.body != null ? __instance.body.position : (Vector2)__instance.myPlayer.transform.position;
			var delta = pos - currentPos;
			if (delta.magnitude > 0.5f)
			{
				__instance.myPlayer.transform.position = pos;
				if (__instance.body != null)
				{
					__instance.body.position = pos;
				}
			}
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
		if (!VesselControlState.IsUsingState(player.PlayerId, out _, out _))
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
