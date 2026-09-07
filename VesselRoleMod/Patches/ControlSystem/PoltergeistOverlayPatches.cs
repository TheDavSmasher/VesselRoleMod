using HarmonyLib;
using InnerNet;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers.Impostor;
using TownOfUs.Modifiers.Impostor.Herbalist;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules;
using TownOfUs.Modules.Components;
using TownOfUs.Options;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Patches.ControlSystem;

[HarmonyPatch]
public static class PoltergeistOverlayPatches
{
	private static readonly Dictionary<byte, Vector3> _colorBlindBasePos = [];

	[HarmonyPatch(typeof(HudManagerHelper), "GetRoleNameText")]
	[HarmonyPrefix]
	public static void GhostHudManagerUpdatePrefix(bool inMeeting, ref bool localDead, ref bool localGhost, bool isVisible)
	{
		if (!localDead || inMeeting || !isVisible)
		{
			return;
		}

		if (!PlayerControl.LocalPlayer.HasModifier<PoltergeistModifier>())
		{
			return;
		}

		localDead = false;
		localGhost = false;
	}

	[HarmonyPatch(typeof(EclipsalBlindModifier), nameof(EclipsalBlindModifier.FixedUpdate))]
	[HarmonyPatch(typeof(MedicShieldModifier), nameof(MedicShieldModifier.FixedUpdate))]
	[HarmonyPatch(typeof(SwoopModifier), nameof(SwoopModifier.FixedUpdate))]
	[HarmonyPostfix]
	public static void VisionModifiersFixedUpdatePostfix(TimedModifier __instance)
	{
		var local = PlayerControl.LocalPlayer;
		var genOpt = OptionGroupSingleton<GeneralOptions>.Instance;

		if (!local.Data.IsDead || !genOpt.TheDeadKnow || MeetingHud.Instance)
		{
			return;
		}

		if (!local.HasModifier<PoltergeistModifier>())
		{
			return;
		}

		if (__instance is EclipsalBlindModifier blindMod && !PlayerControl.LocalPlayer.IsImpostorAligned())
		{
			blindMod.Player.cosmetics.currentBodySprite.BodySprite.material.SetColor(ShaderID.VisorColor, Color.black);
			blindMod.EclipseBack?.SetActive(!blindMod.Player.IsVisibleToOthers());
		}
		if (__instance is SwoopModifier swoopMod && !PlayerControl.LocalPlayer.IsImpostorAligned())
		{
			var appearance = swoopMod.GetVisualAppearance();
			appearance.RendererColor = Color.clear;
			swoopMod.Player.RawSetAppearance(appearance);
		}

		if (__instance is MedicShieldModifier medicMod)
		{
			medicMod.MedicShield?.SetActive(false);
		}
	}

	[HarmonyPatch(typeof(HerbalistProtectionModifier), nameof(HerbalistProtectionModifier.Update))]
	[HarmonyPatch(typeof(WardenFortifiedModifier), nameof(WardenFortifiedModifier.Update))]
	[HarmonyPatch(typeof(ClericBarrierModifier), nameof(ClericBarrierModifier.Update))]
	[HarmonyPatch(typeof(MagicMirrorModifier), nameof(MagicMirrorModifier.Update))]
	[HarmonyPostfix]
	public static void VisionModifiersUpdatePostfix(TimedModifier __instance)
	{
		var local = PlayerControl.LocalPlayer;
		var genOpt = OptionGroupSingleton<GeneralOptions>.Instance;

		if (!local.Data.IsDead || !genOpt.TheDeadKnow || MeetingHud.Instance)
		{
			return;
		}

		if (!local.HasModifier<PoltergeistModifier>())
		{
			return;
		}

		if (__instance is GuardianAngelProtectModifier protectMod)
		{
			for (var i = protectMod.Player.currentRoleAnimations.Count - 1; i >= 0; i--)
			{
				if (protectMod.Player.currentRoleAnimations[i] != null && protectMod.Player.currentRoleAnimations[i].effectType ==
					RoleEffectAnimation.EffectType.ProtectLoop)
				{
					protectMod.Player.currentRoleAnimations[i].gameObject.SetActive(false);
				}
			}
		}
		if (__instance is WardenFortifiedModifier fortMod)
		{
			fortMod.WardenFort?.SetActive(false);
		}
		if (__instance is HerbalistProtectionModifier herbMod)
		{
			herbMod.ClericBarrier?.SetActive(false);
		}
		if (__instance is ClericBarrierModifier clericMod)
		{
			clericMod.ClericBarrier?.SetActive(false);
		}
		if (__instance is MagicMirrorModifier mirrorMod)
		{
			mirrorMod.MedicShield?.SetActive(false);
		}
	}

	[HarmonyPatch(typeof(Bomb), nameof(Bomb.BombShowTeammate))]
	[HarmonyPrefix]
	public static bool BombShowTeammatePrefix(PlayerControl player)
	{
		if (player.HasModifier<PoltergeistModifier>() && !player.IsImpostorAligned())
		{
			return false;
		}
		return true;
	}

	[HarmonyPatch(typeof(EscapistRole), nameof(EscapistRole.FixedUpdate))]
	[HarmonyPostfix]
	public static void EscapistFixedUpdatePostfix(EscapistRole __instance)
	{
		if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
		{
			return;
		}

		var local = PlayerControl.LocalPlayer;
		var genOpt = OptionGroupSingleton<GeneralOptions>.Instance;

		if (!local.Data.IsDead || !genOpt.TheDeadKnow || MeetingHud.Instance)
		{
			return;
		}

		if (!local.HasModifier<PoltergeistModifier>())
		{
			return;
		}

		if (__instance.Player.IsImpostorAligned())
		{
			return;
		}

		__instance.EscapeMark?.SetActive(false);
	}

	[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
	[HarmonyPriority(Priority.Last)]
	[HarmonyPostfix]
	public static void PoltergeistHideGhosts()
	{
		if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
		{
			return;
		}

		if (!PlayerControl.LocalPlayer.Data.IsDead)
		{
			return;
		}

		if (MeetingHud.Instance)
		{
			return;
		}

		if (!OptionGroupSingleton<PostmortemOptions>.Instance.TheDeadKnow)
		{
			return;
		}

		if (!PlayerControl.LocalPlayer.HasModifier<PoltergeistModifier>())
		{
			return;
		}

		foreach (var player in PlayerControl.AllPlayerControls)
		{
			if (player.AmOwner)
			{
				continue;
			}

			if (!player.Data.IsDead)
			{
				continue;
			}

			switch (player.Data.Role)
			{
				case SpectreRole { Caught: false }:
				case HaunterRole { Caught: false }:
					continue;
			}

			var show = false;
			var bodyForms = player.gameObject.transform.FindChildObject("BodyForms");

			foreach (var form in bodyForms.GetAllChildren())
			{
				if (form.activeSelf)
				{
					form.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, show ? 1f : 0f);
				}
			}

			if (player.cosmetics.HasPetEquipped())
			{
				player.cosmetics.CurrentPet.Visible = show;
			}

			player.cosmetics.gameObject.SetActive(show);
			player.gameObject.transform.FindChildObject("Names").SetActive(show);
		}
	}

	[HarmonyPatch(typeof(OpenDoorConsole), nameof(OpenDoorConsole.SetOutline))]
	[HarmonyPatch(typeof(PlatformConsole), nameof(PlatformConsole.SetOutline))]
	[HarmonyPatch(typeof(ZiplineConsole), nameof(ZiplineConsole.SetOutline))]
	[HarmonyPatch(typeof(DeconControl), nameof(DeconControl.SetOutline))]
	[HarmonyPatch(typeof(DoorConsole), nameof(DoorConsole.SetOutline))]
	[HarmonyPatch(typeof(Ladder), nameof(Ladder.SetOutline))]
	[HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
	[HarmonyPriority(Priority.Last)]
	[HarmonyPostfix]
	public static void SetOutlinePostfix(MonoBehaviour __instance, bool on, bool mainTarget)
	{
		if (__instance.TryCast<IUsable>() == null)
		{
			return;
		}

		if (!PlayerControl.LocalPlayer.HasModifierOfType<IVesselPossessModifier>() ||
			!VesselControlState.IsUsingState(PlayerControl.LocalPlayer.PlayerId, out _, out _))
		{
			return;
		}

		if (!GetImageToOutline(__instance, out SpriteRenderer? image, out Color color, out float onVal))
		{
			return;
		}

		image.material.SetFloat(ShaderID.Outline, on ? onVal : 0);
		image.material.SetColor(ShaderID.OutlineColor, color);
		image.material.SetColor(ShaderID.AddColor, mainTarget ? color : Color.clear);
	}

	private static bool GetImageToOutline(MonoBehaviour __instance, [NotNullWhen(true)] out SpriteRenderer? image, out Color color, out float onVal)
	{
		onVal = 1f;
		color = Color.white;
		if (__instance.TryCast<Ladder>() is { } ladder)
		{
			image = ladder.Image;
		}
		else if (__instance.TryCast<ZiplineConsole>() is { } zipline)
		{
			image = zipline.image;
		}
		else if (__instance.TryCast<DeconControl>() is { } decon && decon.Image)
		{
			image = decon.Image;
		}
		else if (__instance.TryCast<DoorConsole>() is { } doorConsole && doorConsole.Image)
		{
			image = doorConsole.Image;
		}
		else if (__instance.TryCast<OpenDoorConsole>() is { } openDoorConsole && openDoorConsole.image)
		{
			image = openDoorConsole.image;
		}
		else if (__instance.TryCast<PlatformConsole>() is { } platform && platform.Image)
		{
			image = platform.Image;
		}
		else if (__instance.TryCast<Vent>() is { } vent)
		{
			image = vent.myRend;
			color = VesselRoleModColors.Vessel;
		}
		else
		{
			image = null;
			return false;
		}

		if (!VesselControlState.HasControl(PlayerControl.LocalPlayer.PlayerId))
		{
			color = Palette.DisabledGrey;
			onVal = 0.3f;
		}

		return true;
	}
}