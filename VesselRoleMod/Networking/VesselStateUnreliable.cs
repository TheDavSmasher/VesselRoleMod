using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Extensions;
using Reactor.Networking.Rpc;
using TownOfUs.Modules;
using UnityEngine;
using VesselRoleMod.Modules.ControlSystem;

namespace VesselRoleMod.Networking;

internal readonly struct VesselStatePacket
{
	public VesselStatePacket(byte playerId, Vector2 position, Vector2 velocity)
	{
		PlayerId = playerId;
		Position = position;
		Velocity = velocity;
	}

	public byte PlayerId { get; }
	public Vector2 Position { get; }
	public Vector2 Velocity { get; }
}

[RegisterCustomRpc((uint)VesselModInternalRpc.VesselStateUnreliable)]
internal sealed class VesselStateUnreliableRpc(VesselRoleModPlugin plugin, uint id)
	: PlayerCustomRpc<VesselRoleModPlugin, VesselStatePacket>(plugin, id)
{
	public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;
	public override SendOption SendOption => (SendOption)1;

	public override void Write(MessageWriter writer, VesselStatePacket data)
	{
		writer.Write(data.PlayerId);
		writer.Write(data.Position);
		writer.Write(data.Velocity);
	}

	public override VesselStatePacket Read(MessageReader reader)
	{
		var playerId = reader.ReadByte();
		var pos = reader.ReadVector2();
		var vel = reader.ReadVector2();
		return new VesselStatePacket(playerId, pos, vel);
	}

	public override void Handle(PlayerControl sender, VesselStatePacket data)
	{
		var controlledPlayerInfo = GameData.Instance?.GetPlayerById(data.PlayerId);
		var controlled = controlledPlayerInfo?.Object;
		if (controlled == null || sender == null)
		{
			return;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return;
		}

		if (!VesselControlState.IsControlled(data.PlayerId, out _))
		{
			return;
		}

		VesselControlState.SetMovementState(data.PlayerId, data.Position, data.Velocity);
	}
}
