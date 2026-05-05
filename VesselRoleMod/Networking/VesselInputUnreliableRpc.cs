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
	public VesselInputPacket(byte ghostId, Vector2 direction)
	{
		GhostId = ghostId;
		Direction = direction;
	}

	public byte GhostId { get; }
	public Vector2 Direction { get; }
}

[RegisterCustomRpc((uint)VesselModInternalRpc.VesselInputUnreliable)]
internal sealed class VesselInputUnreliableRpc(VesselRoleModPlugin plugin, uint id)
	: PlayerCustomRpc<VesselRoleModPlugin, VesselInputPacket>(plugin, id)
{
	public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;
	public override SendOption SendOption => (SendOption)1;

	public override void Write(MessageWriter writer, VesselInputPacket data)
	{
		writer.Write(data.GhostId);
		writer.Write(data.Direction);
	}

	public override VesselInputPacket Read(MessageReader reader)
	{
		var playerId = reader.ReadByte();
		var dir = reader.ReadVector2();
		return new VesselInputPacket(playerId, dir);
	}

	public override void Handle(PlayerControl sender, VesselInputPacket data)
	{
		var controlledPlayerInfo = GameData.Instance?.GetPlayerById(data.GhostId);
		var controlled = controlledPlayerInfo?.Object;
		if (controlled == null || sender == null)
		{
			return;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return;
		}
		
		if (!VesselControlState.IsControlling(data.GhostId, out var controlledId) ||
			controlledId != sender.PlayerId)
		{
			return;
		}

		VesselControlState.SetSelfDirection(data.GhostId, data.Direction);
	}
}
