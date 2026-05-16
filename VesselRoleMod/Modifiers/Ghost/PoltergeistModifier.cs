using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Patches;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Buttons.Modifiers;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class PoltergeistModifier(PlayerControl vessel) : VesselSeekingModifier(vessel), IVesselModifier
{
	public override string ModifierName => "Ghost Possessor";
	public PlayerControl Target => Vessel;
	public PlayerControl Ghost => Player;
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;

	private LobbyNotificationMessage? controllerNotification;

	public bool CanKill()
	{
		return OptionGroupSingleton<VesselOptions>.Instance.KillingGhostsCanKill && 
			   Vessel.Data.Role is VesselRole && 
			   Player.GetRoleWhenAlive().HasKillingAbility();
	}

	public override void OnActivate()
	{
		if (!Player.AmOwner)
		{
			return;
		}

		SetVisibility(false);
		SetGhostVisibility(false);
		Player.gameObject.layer = LayerMask.NameToLayer("Players");

		var button = CustomButtonSingleton<PoltergeistPossessButton>.Instance;

		if (button != null && !button.EffectActive && Player.AmOwner)
		{
			button.OnSuccess();
		}

		if (Minigame.Instance && Minigame.Instance.TryCast<HauntMenuMinigame>())
		{
			Minigame.Instance.Close();
		}
		HudManager.Instance.AbilityButton.SetDisabled();
		HudManagerPatches.ResetZoom();

		try { Ghost.NetTransform.Halt(); } catch { /* ignored */ }
		if (HudManager.InstanceExists && HudManager.Instance != null)
		{
			HudManager.Instance.PlayerCam.SetTarget(Target);
			HudManager.Instance.ShadowQuad.gameObject.SetActive(true);
		}
		try
		{
			if (Ghost.lightSource != null && Target != null)
			{
				Ghost.lightSource.transform.SetParent(Target.transform);
				Ghost.lightSource.Initialize(Target.Collider.offset / 2f);
			}
		}
		catch { /* ignored */ }
	}

	public override void OnDeactivate()
	{
		if (!Player.AmOwner)
		{
			return;
		}

		SetVisibility(true);
		SetGhostVisibility(true);
		Player.gameObject.layer = LayerMask.NameToLayer("Ghost");

		var button = CustomButtonSingleton<PoltergeistPossessButton>.Instance;

		if (button != null && button.EffectActive)
		{
			button.ResetCooldownAndOrEffect();
		}
		HudManager.Instance.AbilityButton.SetEnabled();
		HudManagerPatches.ZoomButton.SetActive(HudManagerPatches.CanZoom);

		Ghost.moveable = true;
		try { Ghost.NetTransform.Halt(); } catch { /* ignored */ }
		if (HudManager.InstanceExists && HudManager.Instance != null)
		{
			HudManager.Instance.PlayerCam.SetTarget(Ghost);
			HudManager.Instance.ShadowQuad.gameObject.SetActive(false);
		}
		try
		{
			if (Ghost.lightSource != null)
			{
				Ghost.lightSource.transform.SetParent(Ghost.transform);
				Ghost.lightSource.Initialize(Ghost.Collider.offset / 2f);
			}
		}
		catch { /* ignored */ }
	}

	private static void SetGhostVisibility(bool visible)
	{
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

			if (player.Data.Role is IGhostRole { GhostActive: true })
			{
				continue;
			}

			var bodyForms = player.gameObject.transform.GetChild(1).gameObject;

			foreach (var form in bodyForms.GetAllChildren())
			{
				if (form.activeSelf)
				{
					form.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0f);
				}
			}

			player.Visible = visible;
			player.cosmetics.gameObject.SetActive(visible);
			player.gameObject.transform.GetChild(3).gameObject.SetActive(visible);
			foreach (var visibilityItem in player.visibilityItems)
			{
				visibilityItem.Visible = visible;
			}
		}
	}

	private void SetVisibility(bool visible)
	{
		var alpha = visible ? 1f : 0.5f;
		var hatAlpha = visible ? 0.5f : 0.25f;

		var bodySprite = Player.cosmetics.currentBodySprite.BodySprite;
		bodySprite.color = bodySprite.color.SetAlpha(alpha);
		Player.cosmetics.skin.layer.color = Player.cosmetics.skin.layer.color.SetAlpha(alpha);
		Player.cosmetics.ToggleNameVisible(visible);
		Player.SetHatAndVisorAlpha(hatAlpha);
	}

	public override void OnMeetingStart()
	{
		ModifierComponent?.RemoveModifier(this);

		if (Player.AmOwner)
		{
			VesselRole.RpcGhostEndPossession(Player, Vessel);
		}
	}

	public override void FixedUpdate()
	{
		if (Player == null || Player.Data == null || !Player.HasDied() || !Player.AmOwner)
		{
			return;
		}

		if (Vessel == null)
		{
			return;
		}

		if (Vessel.Data == null || Vessel.HasDied() || Vessel.Data.Disconnected || !Player.HasDied())
		{
			VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, Vessel);
			return;
		}

		SetGhostVisibility(false);

		base.FixedUpdate();
	}

	public override void OnTimerComplete()
	{
		if (Player.AmOwner)
		{
			VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, Vessel);
		}
	}

	public void CreateNotification()
	{
		if (Vessel == null || Player == null || !Player.AmOwner)
		{
			return;
		}

		if (controllerNotification == null)
		{
			var controllerText = TouLocale.GetParsed("PoltergeistControlNotif", $"You are possessing {Vessel.Data.PlayerName}!");
			controllerNotification = Helpers.CreateAndShowNotification(
				$"<b>{VesselRoleModColors.Vessel.ToTextColor()}{controllerText.Replace("<player>", Vessel.Data.PlayerName)}</color></b>",
				Color.white, new Vector3(0f, 2f, -20f), spr: VesselRoleIcons.Vessel.LoadAsset());
			controllerNotification?.AdjustNotification();
		}
	}

	public void ClearNotification()
	{
		if (controllerNotification != null && controllerNotification.gameObject != null)
		{
			controllerNotification.gameObject.Destroy();
			controllerNotification = null;
		}
	}
}
