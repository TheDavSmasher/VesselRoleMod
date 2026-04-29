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
	public VesselInputPacket(byte controlledId, Vector2 direction, Vector2 position, Vector2 velocity)
	{
		ControlledId = controlledId;
		Direction = direction;
		Position = position;
		Velocity = velocity;
	}

	public byte ControlledId { get; }
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
		writer.Write(data.ControlledId);
		writer.Write(data.Direction);
		writer.Write(data.Position);
		writer.Write(data.Velocity);
	}

	public override VesselInputPacket Read(MessageReader reader)
	{
		var controlledId = reader.ReadByte();
		var dir = reader.ReadVector2();
		var pos = reader.ReadVector2();
		var vel = reader.ReadVector2();
		return new VesselInputPacket(controlledId, dir, pos, vel);
	}

	public override void Handle(PlayerControl sender, VesselInputPacket data)
	{
		var controlledPlayerInfo = GameData.Instance?.GetPlayerById(data.ControlledId);
		var controlled = controlledPlayerInfo?.Object;
		if (controlled == null)
		{
			return;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return;
		}

		if (sender == null ||
			!VesselControlState.IsControlled(data.ControlledId, out var controllerId) ||
			controllerId != sender.PlayerId)
		{
			return;
		}

		VesselControlState.SetDirection(data.ControlledId, data.Direction);
		VesselControlState.SetMovementState(data.ControlledId, data.Position, data.Velocity);
	}
}
