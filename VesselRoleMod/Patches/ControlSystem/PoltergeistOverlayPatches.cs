using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using MiraAPI.GameOptions;
using MiraAPI.LocalSettings;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using System.Collections.Generic;
using System.Linq;
using TownOfUs;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers.Game.Universal;
using TownOfUs.Modifiers.Impostor;
using TownOfUs.Modifiers.Impostor.Herbalist;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules;
using TownOfUs.Options;
using TownOfUs.Patches;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;
using VesselRoleMod.Modifiers.Ghost;

namespace VesselRoleMod.Patches.ControlSystem;

[HarmonyPatch]
public static class PoltergeistOverlayPatches
{
	private static bool IsLocalPoltergistToBlock()
	{
		var local = PlayerControl.LocalPlayer;
		if (local == null)
		{
			return false;
		}

		if (MeetingHud.Instance)
		{
			return false;
		}

		if (!local.Data.IsDead || !OptionGroupSingleton<GeneralOptions>.Instance.TheDeadKnow)
		{
			return false;
		}

		if (!local.TryGetModifier<PoltergeistModifier>(out var mod))
		{
			return false;
		}

		return true;
	}

	private static readonly Dictionary<byte, Vector3> _colorBlindBasePos = new();

	[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
	[HarmonyPriority(Priority.LowerThanNormal)]
	[HarmonyPostfix]
	public static void GhostHudManagerUpdatePostfix()
	{
		if (!IsLocalPoltergistToBlock())
		{
			return;
		}

		var local = PlayerControl.LocalPlayer;
		if (!local.TryGetModifier<DeathHandlerModifier>(out var deathHandler) || deathHandler.DiedThisRound ||
			!TutorialManager.InstanceExists)
		{
			return;
		}

		var genOpt = OptionGroupSingleton<GeneralOptions>.Instance;
		var taskOpt = OptionGroupSingleton<TaskTrackingOptions>.Instance;

		static PlayerControl GetDisguiseTargetOrSelf(PlayerControl player)
		{
			if (player.TryGetModifier<MorphlingMorphModifier>(out var morph) && morph.Target != null)
			{
				return morph.Target;
			}

			if (player.TryGetModifier<GlitchMimicModifier>(out var mimic) && mimic.Target != null)
			{
				return mimic.Target;
			}

			return player;
		}

		static string GetDiedR1ExtraNameTextForDisplayedIdentity(PlayerControl player)
		{
			var displayPlayer = GetDisguiseTargetOrSelf(player);
			var mod = displayPlayer.GetModifiers<BaseRevealModifier>()
				.FirstOrDefault(x => x.Visible && x is FirstRoundIndicator && x.ExtraNameText != string.Empty);
			return mod?.ExtraNameText ?? string.Empty;
		}

		var colorPlayerNames = LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.ColorPlayerNameToggle.Value;
		var localImp = PlayerControl.LocalPlayer.IsImpostorAligned() &&
					   genOpt is
					   { ImpsKnowRoles.Value: true, FFAImpostorMode: false };
		var localVamp = PlayerControl.LocalPlayer.GetRoleWhenAlive() is VampireRole;
		var useMiraApiChecks = !genOpt.FFAImpostorMode;

		foreach (var player in PlayerControl.AllPlayerControls)
		{
			if (player == null || player.Data == null || player.Data.Role == null)
			{
				continue;
			}

			var revealMods = player.GetModifiers<BaseRevealModifier>().ToList();

			var playerName = player.GetAppearance().PlayerName ?? "Unknown";
			var playerColor = Color.white;

			if (colorPlayerNames && PlayerControl.LocalPlayer.IsImpostorAligned() && player.IsImpostorAligned() &&
				!player.AmOwner && !genOpt.FFAImpostorMode)
			{
				playerColor = Color.red;
			}

			playerColor = playerColor.UpdateTargetColor(player, true);
			playerName = playerName.UpdateTargetSymbols(player, true);
			playerName = playerName.UpdateProtectionSymbols(player, true);
			playerName = playerName.UpdateAllianceSymbols(player, true);
			playerName = playerName.UpdateStatusSymbols(player, true);

			var role = player.Data.Role;
			var customRole = player.Data.Role as ICustomRole;
			var color = Color.white;

			if (role == null)
			{
				continue;
			}

			var roleName = "";
			var impostorBuddy = localImp && player.IsImpostorAligned();
			var vampBuddy = localVamp && role is VampireRole;
			var revealed = revealMods.Any(x => x.Visible && x.RevealRole);
			var localFairy = FairyRole.FairySeesRoleVisibilityFlag(player);
			var localSleuth = SleuthModifier.SleuthVisibilityFlag(player);
			if (player.AmOwner || vampBuddy || impostorBuddy || revealed || localFairy || localSleuth || useMiraApiChecks && customRole != null && customRole.CanLocalPlayerSeeRole(player))
			{
				color = role.TeamColor;
				roleName = $"<size=80%>{color.ToTextColor()}{player.Data.Role.GetRoleName()}</color></size>";

				var revealedRole = revealMods.FirstOrDefault(x => x.Visible && x.RevealRole && x.ShownRole != null);
				if (revealedRole != null)
				{
					color = revealedRole.ShownRole!.TeamColor;
					roleName =
						$"<size=80%>{color.ToTextColor()}{revealedRole.ShownRole!.GetRoleName()}</color></size>";
				}

				if (!player.HasModifier<VampireBittenModifier>() && role is VampireRole && vampBuddy)
				{
					roleName += "<size=80%><color=#FFFFFF> (<color=#A22929>OG</color>)</color></size>";
				}

				if (player.HasModifier<AmbassadorRetrainedModifier>() && impostorBuddy)
				{
					roleName += "<size=80%><color=#FFFFFF> (<color=#D63F42>Retrained</color>)</color></size>";
				}

				var cachedMod = player.GetModifiers<BaseModifier>().FirstOrDefault(x => x is ICachedRole);
				if (cachedMod is ICachedRole cache && cache.Visible &&
					player.Data.Role.GetType() != cache.CachedRole.GetType())
				{
					roleName = cache.ShowCurrentRoleFirst
							? $"<size=80%>{color.ToTextColor()}{player.Data.Role.GetRoleName()}</color> ({cache.CachedRole.TeamColor.ToTextColor()}{cache.CachedRole.GetRoleName()}</color>)</size>"
							: $"<size=80%>{cache.CachedRole.TeamColor.ToTextColor()}{cache.CachedRole.GetRoleName()}</color> ({color.ToTextColor()}{player.Data.Role.GetRoleName()}</color>)</size>";
				}

				if (player.Data.IsDead && role is GuardianAngelRole gaRole)
				{
					roleName = $"<size=80%>{gaRole.TeamColor.ToTextColor()}{TranslationController.Instance.GetString(StringNames.GuardianAngelRole)}</color></size>";
				}

				if (localSleuth || (player.Data.IsDead &&
									role.Role is RoleTypes.CrewmateGhost
										or RoleTypes.ImpostorGhost))
				{
					var roleWhenAlive = player.GetRoleWhenAlive();
					color = roleWhenAlive.TeamColor;

					roleName = $"<size=80%>{color.ToTextColor()}{roleWhenAlive.GetRoleName()}</color></size>";
					if (!player.HasModifier<VampireBittenModifier>() && roleWhenAlive is VampireRole)
					{
						roleName += "<size=80%><color=#FFFFFF> (<color=#A22929>OG</color>)</color></size>";
					}

					if (player.HasModifier<AmbassadorRetrainedModifier>() && player.IsImpostorAligned())
					{
						roleName += "<size=80%><color=#FFFFFF> (<color=#D63F42>Retrained</color>)</color></size>";
					}
				}
			}

			var revealedColorMod = revealMods.FirstOrDefault(x => x.Visible && x.NameColor != null);
			if (revealedColorMod != null)
			{
				playerColor = (Color)revealedColorMod.NameColor!;
				playerName = $"{playerColor.ToTextColor()}{playerName}</color>";
			}

			var addedRoleNameText = revealMods.FirstOrDefault(x => x.Visible && x.ExtraRoleText != string.Empty);
			if (addedRoleNameText != null)
			{
				roleName += $"<size=80%>{addedRoleNameText.ExtraRoleText}</size>";
			}

			if (taskOpt.ShowTaskRound && player.AmOwner &&
					(player.IsCrewmate() ||
					 player.Data.Role is SpectreRole))
			{
				if (roleName != string.Empty)
				{
					roleName += " ";
				}

				roleName += $"<size=80%>{player.TaskInfo()}</size>";
			}

			var addedPlayerNameText = revealMods.FirstOrDefault(x =>
					x.Visible && x.ExtraNameText != string.Empty && x is not FirstRoundIndicator);
			if (addedPlayerNameText != null)
			{
				playerName += addedPlayerNameText.ExtraNameText;
			}

			var diedR1Text = GetDiedR1ExtraNameTextForDisplayedIdentity(player);
			if (!string.IsNullOrEmpty(diedR1Text))
			{
				playerName += diedR1Text;
			}

			if (!string.IsNullOrEmpty(roleName))
			{
				playerName = colorPlayerNames
					? $"{roleName}\n{color.ToTextColor()}{playerName}</color>"
					: $"{roleName}\n{playerName}";
			}

			player.cosmetics.nameText.text = playerName;
			player.cosmetics.nameText.color = playerColor;

			player.cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.15f, -0.5f);

			var cbId = player.PlayerId;
			var cbCurrent = player.cosmetics.colorBlindText.transform.localPosition;
			var cbOffset = Vector3.down * 0.12f;

			if (!_colorBlindBasePos.TryGetValue(cbId, out var cbBase))
			{
				cbBase = string.IsNullOrEmpty(diedR1Text) ? cbCurrent : cbCurrent - cbOffset;
				_colorBlindBasePos[cbId] = cbBase;
			}
			else if (string.IsNullOrEmpty(diedR1Text))
			{
				var cbExpectedNoR1 = cbBase;
				var cbExpectedR1 = cbBase + cbOffset;
				if ((cbCurrent - cbExpectedNoR1).sqrMagnitude > 0.0001f &&
					(cbCurrent - cbExpectedR1).sqrMagnitude > 0.0001f)
				{
					cbBase = cbCurrent;
					_colorBlindBasePos[cbId] = cbBase;
				}
			}

			player.cosmetics.colorBlindText.transform.localPosition =
					string.IsNullOrEmpty(diedR1Text) ? cbBase : cbBase + cbOffset;
		}
	}

	[HarmonyPatch(typeof(EclipsalBlindModifier), nameof(EclipsalBlindModifier.FixedUpdate))]
	[HarmonyPatch(typeof(MedicShieldModifier), nameof(MedicShieldModifier.FixedUpdate))]
	[HarmonyPatch(typeof(SwoopModifier), nameof(SwoopModifier.FixedUpdate))]
	[HarmonyPostfix]
	public static void VisionModifiersFixedUpdatePostfix(TimedModifier __instance)
	{
		if (!IsLocalPoltergistToBlock())
		{
			return;
		}

		if (__instance is EclipsalBlindModifier blindMod && ! PlayerControl.LocalPlayer.IsImpostorAligned())
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
		if (!IsLocalPoltergistToBlock())
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
		if (!IsLocalPoltergistToBlock() || __instance.Player.IsImpostorAligned())
		{
			return;
		}

		__instance.EscapeMark?.SetActive(false);
	}

	[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
	[HarmonyPostfix]
	public static void PoltergeistHideGhosts()
	{
		if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
		{
			return;
		}

		if (!IsLocalPoltergistToBlock())
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

			var show = LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.DeadSeeGhostsToggle.Value;
			var bodyForms = player.gameObject.transform.GetChild(1).gameObject;

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
			player.gameObject.transform.GetChild(3).gameObject.SetActive(show);
		}
	}
}
