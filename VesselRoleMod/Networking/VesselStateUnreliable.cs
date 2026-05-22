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
	public VesselStatePacket(byte vesselId, Vector2 position, bool inAnim, Vector2 velocity)
	{
		VesselId = vesselId;
		Position = position;
		InAnim = inAnim;
		Velocity = velocity;
	}

	public byte VesselId { get; }
	public Vector2 Position { get; }
	public bool InAnim { get; }
	public Vector2 Velocity { get; }
}

[RegisterCustomRpc((uint)VesselModInternalRpc.VesselStateUnreliable)]
internal sealed class VesselStateUnreliableRpc(VesselRoleModPlugin plugin, uint id)
	: PlayerCustomRpc<VesselRoleModPlugin, VesselStatePacket>(plugin, id)
{
	public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

	public override void Write(MessageWriter writer, VesselStatePacket data)
	{
		writer.Write(data.VesselId);
		writer.Write(data.Position);
		writer.Write(data.InAnim);
		writer.Write(data.Velocity);
	}

	public override VesselStatePacket Read(MessageReader reader)
	{
		var vesselId = reader.ReadByte();
		var pos = reader.ReadVector2();
		var anim = reader.ReadBoolean();
		var vel = reader.ReadVector2();
		return new VesselStatePacket(vesselId, pos, anim, vel);
	}

	public override void Handle(PlayerControl sender, VesselStatePacket data)
	{
		var targetPlayerInfo = GameData.Instance?.GetPlayerById(data.VesselId);
		var target = targetPlayerInfo?.Object;
		if (target == null || sender == null)
		{
			return;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return;
		}

		if (!VesselControlState.IsControlled(data.VesselId, out _) ||
			data.VesselId != sender.PlayerId)
		{
			return;
		}

		VesselControlState.SetMovementState(data.VesselId, data.Position, data.InAnim, data.Velocity);		
	}
}
