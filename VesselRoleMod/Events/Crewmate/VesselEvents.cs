using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using System.Collections;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Events.TouEvents;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modifiers.Impostor;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Buttons.Crewmate;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Events.Crewmate;

public static class VesselEvents
{
	[RegisterEvent]
	public static void VotingCompleteHandler(VotingCompleteEvent _)
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
	public static void AfterMurderEventHandler(AfterMurderEvent @event)
	{
		var source = @event.Source;

		if (!source.Data.IsDead || !source.TryGetModifier<PoltergeistModifier>(out var mod))
		{
			return;
		}

		if (mod.Vessel.TryGetModifier<AllianceGameModifier>(out var allyMod) && !allyMod.GetsPunished)
		{
			return;
		}

		var target = @event.Target;

		if (PossessionHistory.VesselStats.TryGetValue(mod.Vessel.PlayerId, out var stats))
		{
			if (!target.IsCrewmate() ||
				(target.TryGetModifier<AllianceGameModifier>(out var allyMod2) && !allyMod2.GetsPunished))
			{
				stats.GhostCorrectKills += 1;
			}
			else if (source != target)
			{
				stats.GhostIncorrectKills += 1;
			}
		}
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
	public static void HunterStalkVesselHandler(TouAbilityEvent @event)
	{
		if (@event.AbilityType != AbilityType.HunterStalk)
		{
			return;
		}

		if (@event.Target?.TryCast<PlayerControl>() is not { } target ||
			!target.TryGetModifier<VesselPossessedModifier>(out var mod))
		{
			return;
		}

		mod.Ghost.AddModifier<HunterStalkedModifier>(@event.Player);
	}

	[RegisterEvent]
	public static void GlitchHackVesselHandler(TouAbilityEvent @event)
	{
		if (@event.AbilityType != AbilityType.GlitchInitialHack)
		{
			return;
		}

		if (@event.Target?.TryCast<PlayerControl>() is not { } target ||
			!target.TryGetModifier<VesselPossessedModifier>(out var mod))
		{
			return;
		}

		mod.Ghost.AddModifier<GlitchHackedModifier>(@event.Player.PlayerId);
	}

	[RegisterEvent]
	public static void HackedShowVesselHandler(TouAbilityEvent @event)
	{
		if (@event.AbilityType != AbilityType.GlitchHackTrigger)
		{
			return;
		}

		if (@event.Target?.TryCast<PlayerControl>() is not { } target ||
			!target.TryGetModifierOfType<IVesselPossessModifier>(out var mod))
		{
			return;
		}

		mod.Target.GetModifier<GlitchHackedModifier>()!.ShowHacked();
	}

	[RegisterEvent]
	public static void ClericCleanseVesselHandler(TouAbilityEvent @event)
	{
		if (@event.AbilityType != AbilityType.ClericCleanse)
		{
			return;
		}

		if (@event.Target?.TryCast<PlayerControl>() is not { } target ||
			!target.TryGetModifier<VesselPossessedModifier>(out var mod))
		{
			return;
		}

		var effects = ClericCleanseModifier.FindNegativeEffects(mod.Ghost);
		if (effects.Contains(ClericCleanseModifier.EffectType.Hack))
		{
			mod.Ghost.RemoveModifier<GlitchHackedModifier>();
		}
		if (effects.Contains(ClericCleanseModifier.EffectType.Blind))
		{
			mod.Ghost.RpcRemoveModifier<EclipsalBlindModifier>();
		}

		if (effects.Contains(ClericCleanseModifier.EffectType.Flash))
		{
			mod.Ghost.RemoveModifier<GrenadierFlashModifier>();
		}

		if (effects.Contains(ClericCleanseModifier.EffectType.Hypnosis))
		{
			mod.Ghost.RemoveModifier<HypnotisedModifier>();
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
