using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Extensions;
using UnityEngine;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Networking;

internal readonly record struct VesselStatePacket(byte TargetId, Vector2 Position, bool InAnim, Vector2 Velocity) : ITargetDataPacket;

[RegisterCustomRpc((uint)VesselModInternalRpc.VesselStateUnreliable)]
internal sealed class VesselStateUnreliableRpc(VesselRoleModPlugin plugin, uint id)
	: ControlDataUnreliableRpc<VesselStatePacket>(plugin, id)
{
	public override void Write(MessageWriter writer, VesselStatePacket data)
	{
		writer.Write(data.TargetId);
		writer.Write(data.Position);
		writer.Write(data.InAnim);
		writer.Write(data.Velocity);
	}

	public override VesselStatePacket Read(MessageReader reader)
	{
		var vesselId = reader.ReadByte();
		var pos = reader.ReadVector2();
		var anim = reader.ReadBoolean();
		var vel = reader.ReadVector2().ApplyDeadzone();
		return new VesselStatePacket(vesselId, pos, anim, vel);
	}

	protected override void Store(PlayerControl sender, VesselStatePacket data)
	{
		if (!VesselControlState.IsControlled(data.TargetId, out _) ||
			data.TargetId != sender.PlayerId)
		{
			return;
		}

		VesselControlState.SetMovementState(data.TargetId, data.Position, data.InAnim, data.Velocity);
	}
}
