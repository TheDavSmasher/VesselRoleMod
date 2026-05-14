using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Extensions;
using Reactor.Networking.Rpc;
using TownOfUs.Modules;
using UnityEngine;
using VesselRoleMod.Modules.ControlSystem;

namespace VesselRoleMod.Networking;

internal readonly struct VesselInputPacket
{
	public VesselInputPacket(byte targetId, bool fromVessel, Vector2 direction, Vector2 position, Vector2 velocity)
	{
		TargetId = targetId;
		FromVessel = fromVessel;
		Direction = direction;
		Position = position;
		Velocity = velocity;
	}

	public byte TargetId { get; }
	public bool FromVessel { get; }
	public Vector2 Direction { get; }
	public Vector2 Position { get; }
	public Vector2 Velocity { get; }
}

[RegisterCustomRpc((uint)VesselModInternalRpc.VesselInputUnreliable)]
internal sealed class VesselInputUnreliableRpc(VesselRoleModPlugin plugin, uint id)
	: PlayerCustomRpc<VesselRoleModPlugin, VesselInputPacket>(plugin, id)
{
	public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;
	public override SendOption SendOption => (SendOption)1;

	public override void Write(MessageWriter writer, VesselInputPacket data)
	{
		writer.Write(data.TargetId);
		writer.Write(data.FromVessel);
		writer.Write(data.Direction);
		writer.Write(data.Position);
		writer.Write(data.Velocity);
	}

	public override VesselInputPacket Read(MessageReader reader)
	{
		var playerId = reader.ReadByte();
		var fromV = reader.ReadBoolean();
		var dir = reader.ReadVector2();
		var pos = reader.ReadVector2();
		var vel = reader.ReadVector2();
		return new VesselInputPacket(playerId, fromV, dir, pos, vel);
	}

	public override void Handle(PlayerControl sender, VesselInputPacket data)
	{
		var targetPlayerInfo = GameData.Instance?.GetPlayerById(data.TargetId);
		var target = targetPlayerInfo?.Object;
		if (target == null || sender == null)
		{
			return;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return;
		}

		if (data.FromVessel)
		{
			if (!VesselControlState.IsControlling(data.TargetId, out var vesselId) ||
				vesselId != sender.PlayerId)
			{
				return;
			}
			VesselControlState.SetSelfDirection(data.TargetId, data.Direction);
			VesselControlState.SetMovementState(vesselId, data.Position, data.Velocity);
		}
		else
		{
			if (!VesselControlState.IsControlled(data.TargetId, out var ghostId) ||
				ghostId != sender.PlayerId)
			{
				return;
			}

			VesselControlState.SetForcedDirection(data.TargetId, data.Direction);
			VesselControlState.SetMovementState(data.TargetId, data.Position, data.Velocity);
		}
	}
}
