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
	public PoltergeistInputPacket(byte controlledId, Vector2 direction, Vector2 position, Vector2 velocity)
	{
		ControllingId = controlledId;
		Direction = direction;
		Position = position;
		Velocity = velocity;
	}

	public byte ControllingId { get; }
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
		writer.Write(data.ControllingId);
		writer.Write(data.Direction);
		writer.Write(data.Position);
		writer.Write(data.Velocity);
	}

	public override PoltergeistInputPacket Read(MessageReader reader)
	{
		var controlledId = reader.ReadByte();
		var dir = reader.ReadVector2();
		var pos = reader.ReadVector2();
		var vel = reader.ReadVector2();
		return new PoltergeistInputPacket(controlledId, dir, pos, vel);
	}

	public override void Handle(PlayerControl sender, PoltergeistInputPacket data)
	{
		var controllingPlayerInfo = GameData.Instance?.GetPlayerById(data.ControllingId);
		var controlling = controllingPlayerInfo?.Object;
		if (controlling == null)
		{
			return;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return;
		}

		if (sender == null ||
			!VesselControlState.IsControlling(data.ControllingId, out var controlledId) ||
			controlledId != sender.PlayerId)
		{
			return;
		}

		VesselControlState.SetSelfDirection(data.ControllingId, data.Direction);
		VesselControlState.SetSelfMovementState(data.ControllingId, data.Position, data.Velocity);
	}
}
