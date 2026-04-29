using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using Reactor.Networking.Attributes;
using TownOfUs.Utilities;
using VesselRoleMod.Buttons.Modifiers;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class PoltergeistModifier(PlayerControl vessel) : VesselSeekingModifier(vessel)
{
	public override string ModifierName => "Ghost Possessor";

	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;

	public bool CanKill()
	{
		return Vessel.Data.Role is VesselRole;
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();

		var button = CustomButtonSingleton<PoltergeistPossessButton>.Instance;

		if (button != null && button.EffectActive)
		{
			button.ResetCooldownAndOrEffect();
		}
	}

	[MethodRpc((uint)VesselModRpc.AdorcismStart)]
	public static void RpcSeekVessel(PlayerControl player, PlayerControl target)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(player);
			return;
		}
		if (player.HasModifier<ValidAdorcismGhostModifier>(x => x.Vessel.PlayerId == target.PlayerId))
		{
			Error("RpcSeekVessel - Invalid ghost");
			return;
		}
		if (target.Data.Role is not VesselRole)
		{
			Error("RpcSeekVessel - Invalid Vessel target");
			return;
		}

		player.AddModifier<ValidAdorcismGhostModifier>(target);
	}

	[MethodRpc((uint)VesselModRpc.AdorcismEnd)]
	public static void RpcVesselClosed(PlayerControl player, PlayerControl target)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(player);
			return;
		}
		if (target.Data.Role is not VesselRole)
		{
			Error("RpcVesselClosed - Invalid Vessel target");
			return;
		}

		if (player.TryGetModifier<ValidAdorcismGhostModifier>(out var mod, x => x.Vessel.PlayerId == target.PlayerId))
		{
			player.RemoveModifier(mod);
		}
		else
		{
			Error("RpcVesselClosed - Invalid ghost");
		}
	}

	[MethodRpc((uint)VesselModRpc.Possess)]
	public static void RpcPossess(PlayerControl player, PlayerControl target)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(player);
			return;
		}
		if (!player.HasModifier<ValidAdorcismGhostModifier>(x => x.Vessel.PlayerId == target.PlayerId))
		{
			Error("RpcPossess - Invalid poltergeist");
			return;
		}
		if (target.Data.Role is not VesselRole)
		{
			Error("RpcPossess - Invalid Vessel target");
			return;
		}

		player.AddModifier<PoltergeistModifier>(target);
		target.AddModifier<VesselPossessedModifier>(player);
	}
}
