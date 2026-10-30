using Reactor.Networking.Rpc;
using TownOfUs.Modules;
using VesselRoleMod.Modules.ControlSystem;

namespace VesselRoleMod.Networking;

internal interface ITargetDataPacket
{
	byte TargetId { get; }
}

internal abstract class ControlDataUnreliableRpc<TData>(VesselRoleModPlugin plugin, uint id) :
	PlayerCustomRpc<VesselRoleModPlugin, TData>(plugin, id) where TData : ITargetDataPacket
{
	public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

	public override void Handle(PlayerControl sender, TData? data)
	{
		if (data == null)
		{
			return;
		}

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

		if (!VesselControlState.IsUsingState(data.TargetId, out var otherId, out var isControlled) ||
			otherId != sender.PlayerId)
		{
			return;
		}

		Store(isControlled, data);
	}

	protected abstract void Store(bool isControlled, TData data);
}
