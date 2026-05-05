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
	public VesselInputPacket(byte playerId, bool controlled, Vector2 direction)
	{
		PlayerId = playerId;
		Controlled = controlled;
		Direction = direction;
	}

	public byte PlayerId { get; }
	public bool Controlled { get; }
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
		writer.Write(data.PlayerId);
		writer.Write(data.Controlled);
		writer.Write(data.Direction);
	}

	public override VesselInputPacket Read(MessageReader reader)
	{
		var playerId = reader.ReadByte();
		var controlled = reader.ReadBoolean();
		var dir = reader.ReadVector2();
		return new VesselInputPacket(playerId, controlled, dir);
	}

	public override void Handle(PlayerControl sender, VesselInputPacket data)
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

		if (data.Controlled)
		{
			if (!VesselControlState.IsControlled(data.PlayerId, out var controllerId) ||
				controllerId != sender.PlayerId)
			{
				return;
			}
			VesselControlState.SetForcedDirection(data.PlayerId, data.Direction);
			return;
		}
		
		if (!VesselControlState.IsControlling(data.PlayerId, out var controlledId) ||
			controlledId != sender.PlayerId)
		{
			return;
		}

		VesselControlState.SetSelfDirection(data.PlayerId, data.Direction);
	}
}
