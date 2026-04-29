using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using System;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Crewmate;
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
	}

	public override void OnMeetingStart()
	{
		RoleBehaviourStubs.OnMeetingStart(this);
	}
}
