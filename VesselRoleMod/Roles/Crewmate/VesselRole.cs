using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using System;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Roles.Crewmate;

public sealed class VesselRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
	public override bool IsAffectedByComms => false;

	public DoomableType DoomHintType => DoomableType.Death;
	public string RoleName => TouLocale.Get("VesselRole");
	public string RoleDescription => TouLocale.GetParsed("VesselRoleIntroBlurb");
	public string RoleLongDescription => TouLocale.GetParsed("VesselRoleTabDescription");

	public string GetAdvancedDescription()
	{
		return
			TouLocale.GetParsed("VesselRoleWikiDescription") +
			MiscUtils.AppendOptionsText(GetType());
	}

	public Color RoleColor => VesselRoleModColors.Vessel;
	public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
	public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;


	public CustomRoleConfiguration Configuration => new(this)
	{
		Icon = null,
		IntroSound = TouAudio.MediumIntroSound
	};

	public override void Initialize(PlayerControl player)
	{
		RoleBehaviourStubs.Initialize(this, player);

		if (OptionGroupSingleton<VesselOptions>.Instance.CanRejectPossession != VesselRejectionType.None && 
			!player.HasModifier<VesselBlacklistModifier>())
		{
			player.AddModifier<VesselBlacklistModifier>();
		}
	}

	public override void Deinitialize(PlayerControl targetPlayer)
	{
		RoleBehaviourStubs.Deinitialize(this, targetPlayer);
		if (targetPlayer.HasModifier<VesselBlacklistModifier>())
		{
			targetPlayer.RemoveModifier<VesselBlacklistModifier>();
		}
	}

	public override void OnMeetingStart()
	{
		RoleBehaviourStubs.OnMeetingStart(this);
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

	[MethodRpc((uint)VesselModRpc.VesselPossession)]
	public static void RpcGhostPossession(PlayerControl ghost, PlayerControl vessel)
	{
		if (LobbyBehaviour.Instance)
		{
			MiscUtils.RunAnticheatWarning(ghost);
			return;
		}
		if (!ghost.HasModifier<ValidAdorcismGhostModifier>(x => x.Vessel.PlayerId == vessel.PlayerId))
		{
			Error("RpcPossess - Invalid poltergeist");
			return;
		}
		if (vessel.Data.Role is not VesselRole)
		{
			Error("RpcPossess - Invalid Vessel target");
			return;
		}

		ghost.AddModifier<PoltergeistModifier>(vessel);
		vessel.AddModifier<VesselPossessedModifier>(ghost);
	}

	[MethodRpc((uint)VesselModRpc.VesselEndPossession)]
	public static void RpcGhostEndPossession(PlayerControl ghost, PlayerControl vessel)
	{

	}

	[MethodRpc((uint)VesselModRpc.VesselTriggerInteraction)]
	public static void RpcGhostTriggerInteraction(PlayerControl ghost, PlayerControl vessel, Vector2 interactablePosition)
	{

	}
}
