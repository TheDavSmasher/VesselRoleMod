using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Extensions;
using Reactor.Networking.Rpc;
using TownOfUs.Modules;
using UnityEngine;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Networking;

internal readonly record struct VesselInputPacket(byte TargetId, bool FromVessel, Vector2 Direction);

[RegisterCustomRpc((uint)VesselModInternalRpc.VesselInputUnreliable)]
internal sealed class VesselInputUnreliableRpc(VesselRoleModPlugin plugin, uint id)
	: PlayerCustomRpc<VesselRoleModPlugin, VesselInputPacket>(plugin, id)
{
	public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

	public override void Write(MessageWriter writer, VesselInputPacket data)
	{
		writer.Write(data.TargetId);
		writer.Write(data.FromVessel);
		writer.Write(data.Direction);
	}

	public override VesselInputPacket Read(MessageReader reader)
	{
		var playerId = reader.ReadByte();
		var fromV = reader.ReadBoolean();
		var dir = reader.ReadVector2().ApplyDeadzone();
		return new VesselInputPacket(playerId, fromV, dir);
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

		}
		else
		{
			if (!VesselControlState.IsControlled(data.TargetId, out var ghostId) ||
				ghostId != sender.PlayerId)
			{
				return;
			}
			VesselControlState.SetForcedDirection(data.TargetId, data.Direction);
		}
	}
}
