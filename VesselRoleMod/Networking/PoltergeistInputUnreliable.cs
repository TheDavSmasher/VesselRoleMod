using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Extensions;
using Reactor.Networking.Rpc;
using TownOfUs.Modules;
using UnityEngine;
using VesselRoleMod.Modules.ControlSystem;

namespace VesselRoleMod.Networking;

internal readonly struct PoltergeistInputPacket
{
	public PoltergeistInputPacket(byte vesselId, Vector2 direction, Vector2 position, Vector2 velocity)
	{
		VesselId = vesselId;
		Direction = direction;
		Position = position;
		Velocity = velocity;
	}

	public byte VesselId { get; }
	public Vector2 Direction { get; }
	public Vector2 Position { get; }
	public Vector2 Velocity { get; }
}

[RegisterCustomRpc((uint)VesselModInternalRpc.PoltergeistInputUnreliable)]
internal sealed class PoltergeistInputUnreliableRpc(VesselRoleModPlugin plugin, uint id)
	: PlayerCustomRpc<VesselRoleModPlugin, PoltergeistInputPacket>(plugin, id)
{
	public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;
	public override SendOption SendOption => (SendOption)1;

	public override void Write(MessageWriter writer, PoltergeistInputPacket data)
	{
		writer.Write(data.VesselId);
		writer.Write(data.Direction);
		writer.Write(data.Position);
		writer.Write(data.Velocity);
	}

	public override PoltergeistInputPacket Read(MessageReader reader)
	{
		var playerId = reader.ReadByte();
		var dir = reader.ReadVector2();
		var pos = reader.ReadVector2();
		var vel = reader.ReadVector2();
		return new PoltergeistInputPacket(playerId, dir, pos, vel);
	}

	public override void Handle(PlayerControl sender, PoltergeistInputPacket data)
	{
		var vesselPlayerInfo = GameData.Instance?.GetPlayerById(data.VesselId);
		var vessel = vesselPlayerInfo?.Object;
		if (vessel == null || sender == null)
		{
			return;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return;
		}

		if (!VesselControlState.IsControlled(data.VesselId, out var ghostId) ||
			ghostId != sender.PlayerId)
		{
			return;
		}

		VesselControlState.SetForcedDirection(data.VesselId, data.Direction);
		VesselControlState.SetMovementState(data.VesselId, data.Position, data.Velocity);
	}
}
