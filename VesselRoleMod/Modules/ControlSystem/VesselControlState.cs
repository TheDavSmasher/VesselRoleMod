using System.Collections.Generic;
using UnityEngine;

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

	public static bool CanShareControl => false; // OptionGroupSingleton<VesselOptions>.Instance.CanShareControl;

	private static readonly Dictionary<byte, byte> ControlledBy = new();
	private static readonly Dictionary<byte, byte> Controlling = new();

	private static readonly Dictionary<byte, bool> InControl = new();

	private static readonly Dictionary<byte, bool> TimerPaused = new();

	private static readonly Dictionary<byte, Vector2> ControlledDirection = new();
	private static readonly Dictionary<byte, Vector2> SelfDirection = new();

	private static readonly Dictionary<byte, Vector2> ControlledPosition = new();

	private static readonly Dictionary<byte, Vector2> ControlledVelocity = new();

	private static readonly Dictionary<byte, float> ControlledSince = new();

	#region Set Control
	public static void SetControl(byte controlledId, byte controllerId)
	{
		ControlledBy[controlledId] = controllerId;
		Controlling[controllerId] = controlledId;

		InControl[controlledId] = CanShareControl;
		InControl[controllerId] = true;

		ControlledDirection[controlledId] = Vector2.zero;
		SelfDirection[controllerId] = Vector2.zero;

		ControlledPosition[controlledId] = Vector2.zero;

		ControlledVelocity[controlledId] = Vector2.zero;

		ControlledSince[controlledId] = Time.time;
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

		ControlledVelocity.Remove(controlledId);

		ControlledSince.Remove(controlledId);
	}
	#endregion

	#region State Check
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

	#region State Control Check
	public static bool HasControl(byte playerId)
	{
		return InControl.TryGetValue(playerId, out bool has) && has;
	}

	public static bool IsFullyControlling(byte playerId)
	{
		return IsControlling(playerId, out byte otherId) && !HasControl(otherId);
	}

	public static void SwapControlOver(byte playerId, byte againstId)
	{
		if (!IsUsingState(playerId, out var withId) || withId != againstId)
		{
			return;
		}

		(InControl[playerId], InControl[againstId]) = (InControl[againstId], InControl[playerId]);
	}
	#endregion

	#endregion

	#region Direction
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

	public static Vector2 GetDirection(byte playerId)
	{
		if (IsControlled(playerId, out var controllerId))
		{
			return GetDirection(controllerId, playerId);
		}
		if (IsControlling(playerId, out var controlledId))
		{
			return GetDirection(playerId, controlledId);
		}
		return Vector2.zero;
	}

	public static Vector2 GetDirection(byte controllerId, byte controlledId)
	{
		if (CanShareControl)
		{
			return ((GetSelfDirection(controllerId) + GetForcedDirection(controlledId)) / 2).normalized;
		}
		if (HasControl(controllerId))
		{
			return GetForcedDirection(controlledId);
		}
		return GetSelfDirection(controllerId);
	}
	#endregion

	#region Movement State
	public static void SetMovementState(byte controlledId, Vector2 position, Vector2 velocity)
	{
		ControlledPosition[controlledId] = position;
		ControlledVelocity[controlledId] = velocity;
	}

	public static Vector2 GetPosition(byte controlledId)
	{
		return ControlledPosition.TryGetValue(controlledId, out var pos) ? pos : Vector2.zero;
	}

	public static Vector2 GetVelocity(byte controlledId)
	{
		return ControlledVelocity.TryGetValue(controlledId, out var vel) ? vel : Vector2.zero;
	}

	#endregion

	#region Time
	public static float GetControlElapsedSeconds(byte controlledId)
	{
		return ControlledSince.TryGetValue(controlledId, out var since) ? Mathf.Max(0f, Time.time - since) : float.PositiveInfinity;
	}

	public static bool IsInInitialGrace(byte controlledId)
	{
		return GetControlElapsedSeconds(controlledId) < InitialControlSyncGraceSeconds;
	}

	public static void SetTimerActive(byte controlledId)
	{
		TimerPaused[controlledId] = false;
	}

	public static void SetTimerPaused(byte controlledId)
	{
		TimerPaused[controlledId] = true;
	}

	public static void ClearTimer(byte controlledId)
	{
		TimerPaused.Remove(controlledId);
	}

	public static bool IsPausingTimer(byte controlledId)
	{
		return TimerPaused.TryGetValue(controlledId, out var paused) && paused;
	}
	#endregion

	#region Clearing
	public static void ClearAll()
	{
		ControlledBy.Clear();
		Controlling.Clear();

		InControl.Clear();

		TimerPaused.Clear();

		ControlledDirection.Clear();
		SelfDirection.Clear();

		ControlledPosition.Clear();

		ControlledVelocity.Clear();

		ControlledSince.Clear();
	}
	#endregion
}
