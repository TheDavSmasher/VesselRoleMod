using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Events.TouEvents;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Buttons.Crewmate;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Events.Crewmate;

public static class VesselEvents
{
	[RegisterEvent]
	public static void VotingCompleteHandler(VotingCompleteEvent @event)
	{
		ModifierUtils.GetActiveModifiers<VesselBlacklistModifier>().Do(x => x.OnVotingComplete());
	}

	[RegisterEvent]
	public static void RoundStartHandler(RoundStartEvent @event)
	{
		if (@event.TriggeredByIntro)
		{
			return;
		}

		var button = CustomButtonSingleton<VesselAdorciseButton>.Instance;
		button.ResetCooldownAndOrEffect();
	}

	[RegisterEvent]
	public static void PoltergeistReviveHandler(PlayerReviveEvent @event)
	{
		var player = @event.Player;

		if (player.TryGetModifier<PoltergeistModifier>(out var mod))
		{
			VesselRole.GhostEndPossession(player, mod.Vessel);
		}
	}

	[RegisterEvent]
	public static void TransportVesselHandler(TouAbilityEvent @event)
	{
		if (@event.AbilityType != AbilityType.TransporterTransport)
		{
			return;
		}

		if (@event.Target != null &&
			@event.Target is PlayerControl t1 &&
			t1.TryGetModifier<VesselPossessedModifier>(out var mod1) &&
			mod1.Ghost.AmOwner)
		{
			ShowGhostTransport();
		}

		if (@event.Target2 != null &&
			@event.Target2 is PlayerControl t2 &&
			t2.TryGetModifier<VesselPossessedModifier>(out var mod2) &&
			mod2.Ghost.AmOwner)
		{
			ShowGhostTransport();
		}
	}

	private static void ShowGhostTransport()
	{
		var notif1 = Helpers.CreateAndShowNotification(
				$"<b>{TownOfUsColors.Transporter.ToTextColor()}{TouLocale.GetParsed("VesselRoleTransportNotif")}</color></b>", Color.white,
				new Vector3(0f, 1f, -20f), spr: TouRoleIcons.Transporter.LoadAsset());

		notif1.AdjustNotification();

		if (Minigame.Instance)
		{
			Minigame.Instance.Close();
			Minigame.Instance.Close();
		}
	}
}

public enum VesselAbilityType
{
	PoltergeistPossess,
	PoltergeistPossessKill,
	AdorciseStart,
	AdorciseEnd,
	AdorcismSuccess
}
