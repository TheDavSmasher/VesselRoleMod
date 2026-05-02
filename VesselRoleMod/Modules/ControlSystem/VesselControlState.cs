using MiraAPI.GameOptions;
using System;
using System.Collections.Generic;
using UnityEngine;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modules.ControlSystem;

/// <summary>
/// Per-client state for Vessel Possession control. This is intentionally client-local:
/// - The Ghost sends the desired direction for a controlled vessel via RPC.
/// - The controlled vessel's owner applies that direction inside a movement patch.
/// </summary>
public static class VesselControlState
{
	// After initial control begins, different clients may briefly disagree on transform state.
	// During this grace window we avoid applying any victim movement input to prevent desync.
	public const float InitialControlSyncGraceSeconds = 1.0f;

	public static bool CanShareControl => OptionGroupSingleton<VesselOptions>.Instance.CanShareControl;

	private static readonly Dictionary<byte, byte> ControlledBy = new();
	private static readonly Dictionary<byte, byte> Controlling = new();

	private static readonly Dictionary<byte, bool> InControl = new();

	private static readonly Dictionary<byte, Vector2> ControlledDirection = new();
	private static readonly Dictionary<byte, Vector2> SelfDirection = new();

	private static readonly Dictionary<byte, Vector2> ControlledPosition = new();
	private static readonly Dictionary<byte, Vector2> SelfPosition = new();

	private static readonly Dictionary<byte, Vector2> ControlledVelocity = new();
	private static readonly Dictionary<byte, Vector2> SelfVelocity = new();

	private static readonly Dictionary<byte, float> ControlledSince = new();
	private static readonly Dictionary<byte, float> ControllingSince = new();

	public static void SetControl(byte controlledId, byte controllerId)
	{
		ControlledBy[controlledId] = controllerId;
		Controlling[controllerId] = controlledId;

		InControl[controlledId] = CanShareControl;
		InControl[controlledId] = true;

		ControlledDirection[controlledId] = Vector2.zero;
		SelfDirection[controllerId] = Vector2.zero;

		ControlledPosition[controlledId] = Vector2.zero;
		SelfPosition[controllerId] = Vector2.zero;

		ControlledVelocity[controlledId] = Vector2.zero;
		SelfVelocity[controllerId] = Vector2.zero;

		ControlledSince[controlledId] = Time.time;
		ControllingSince[controllerId] = Time.time;
	}

	public static void ClearControl(byte controlledId)
	{
		ControlledBy.Remove(controlledId, out var controllerId);
		Controlling.Remove(controllerId);

		InControl.Remove(controlledId);
		InControl.Remove(controllerId);

		ControlledDirection.Remove(controlledId);
		SelfDirection.Remove(controllerId);

		ControlledPosition.Remove(controlledId);
		SelfPosition.Remove(controllerId);

		ControlledVelocity.Remove(controlledId);
		SelfVelocity.Remove(controllerId);

		ControlledSince.Remove(controlledId);
		ControllingSince.Remove(controllerId);
	}

	public static bool IsControlled(byte controlledId, out byte controllerId)
	{
		return ControlledBy.TryGetValue(controlledId, out controllerId);
	}

	public static bool IsControlling(byte controllerId, out byte controlledId)
	{
		return Controlling.TryGetValue(controllerId, out controlledId);
	}

	public static bool IsUsingState(byte playerId, out byte withId)
	{
		return IsControlled(playerId, out withId) || IsControlling(playerId, out withId);
	}

	public static bool HasControlOver(byte playerId, byte againstId)
	{
		return IsUsingState(playerId, out var withId) && withId == againstId &&
				InControl.TryGetValue(playerId, out bool has) && has;
	}

	public static void SwapControlOver(byte playerId, byte againstId)
	{
		if (!IsUsingState(playerId, out var withId) || withId != againstId)
		{
			return;
		}

		(InControl[playerId], InControl[againstId]) = (InControl[againstId], InControl[playerId]);
	}

	public static void SetForcedDirection(byte controlledId, Vector2 direction)
	{
		ControlledDirection[controlledId] = direction;
	}

	public static void SetSelfDirection(byte controllerId, Vector2 direction)
	{
		SelfDirection[controllerId] = direction;
	}

	public static Vector2 GetForcedDirection(byte controlledId)
	{
		return ControlledDirection.TryGetValue(controlledId, out var dir) ? dir : Vector2.zero;
	}

	public static Vector2 GetSelfDirection(byte controllerId)
	{
		return SelfDirection.TryGetValue(controllerId, out var dir) ? dir : Vector2.zero;
	}

	public static Vector2 GetDirection(byte controllerId, byte controlledId)
	{
		return CombineMovementVectors(GetSelfDirection(controllerId), GetForcedDirection(controlledId));
	}

	public static void SetForcedMovementState(byte controlledId, Vector2 position, Vector2 velocity)
	{
		ControlledPosition[controlledId] = position;
		ControlledVelocity[controlledId] = velocity;
	}

	public static void SetSelfMovementState(byte controllerId, Vector2 position, Vector2 velocity)
	{
		ControlledPosition[controllerId] = position;
		ControlledVelocity[controllerId] = velocity;
	}

	public static Vector2 GetForcedPosition(byte controlledId)
	{
		return ControlledPosition.TryGetValue(controlledId, out var pos) ? pos : Vector2.zero;
	}

	public static Vector2 GetSelfPosition(byte controllerId)
	{
		return SelfPosition.TryGetValue(controllerId, out var pos) ? pos : Vector2.zero;
	}

	public static Vector2 GetForcedVelocity(byte controlledId)
	{
		return ControlledVelocity.TryGetValue(controlledId, out var vel) ? vel : Vector2.zero;
	}

	public static Vector2 GetSelfVelocity(byte controllerId)
	{
		return SelfVelocity.TryGetValue(controllerId, out var vel) ? vel : Vector2.zero;
	}

	public static float GetForcedControlElapsedSeconds(byte controlledId)
	{
		return ControlledSince.TryGetValue(controlledId, out var since) ? Mathf.Max(0f, Time.time - since) : float.PositiveInfinity;
	}

	public static float GetSelfControlElapsedSeconds(byte controllerId)
	{
		return ControllingSince.TryGetValue(controllerId, out var since) ? Mathf.Max(0f, Time.time - since) : float.PositiveInfinity;
	}

	public static bool IsInInitialGraceForced(byte controlledId)
	{
		return GetForcedControlElapsedSeconds(controlledId) < InitialControlSyncGraceSeconds;
	}

	public static bool IsInInitialGraceSelf(byte controllerId)
	{
		return GetSelfControlElapsedSeconds(controllerId) < InitialControlSyncGraceSeconds;
	}

	public static void ClearForcedMovementState(byte controlledId)
	{
		ControlledPosition[controlledId] = Vector2.zero;
		ControlledVelocity[controlledId] = Vector2.zero;
	}

	public static void ClearSelfMovementState(byte controllerId)
	{
		SelfPosition[controllerId] = Vector2.zero;
		SelfVelocity[controllerId] = Vector2.zero;
	}

	public static void ClearAll()
	{
		ControlledBy.Clear();
		Controlling.Clear();

		InControl.Clear();

		ControlledDirection.Clear();
		SelfDirection.Clear();

		ControlledPosition.Clear();
		SelfPosition.Clear();

		ControlledVelocity.Clear();
		SelfVelocity.Clear();

		ControlledSince.Clear();
		ControllingSince.Clear();
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
}
