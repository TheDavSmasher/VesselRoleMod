using MiraAPI.Hud;
using MiraAPI.Utilities;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Buttons.Modifiers;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class PoltergeistModifier(PlayerControl vessel) : ActivePossessionModifier<PoltergeistPossessButton>, IVesselSeekingModifier
{
	private static readonly int PlayerLayer = LayerMask.NameToLayer("Players");
	private static readonly int GhostLayer = LayerMask.NameToLayer("Ghost");

	public override string ModifierName => "Ghost Possessor";
	public override PlayerControl Target => Vessel;
	public override PlayerControl Ghost => Player;
	public override PlayerControl Vessel => vessel;

	public override void OnActivate()
	{
		base.OnActivate();

		if (!Player.AmOwner)
		{
			return;
		}

		SetVisibility(false);
		Player.gameObject.layer = PlayerLayer;

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

		CustomButtonSingleton<FakeVentButton>.Instance.Show = false;
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();

		if (!Player.AmOwner)
		{
			return;
		}

		SetVisibility(true);
		Player.gameObject.layer = GhostLayer;

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

		CustomButtonSingleton<FakeVentButton>.Instance.Show = true;
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
		base.OnMeetingStart();

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

		var vesselInAnim = Vessel.IsInTargetingAnimState() || Vessel.inVent;
		if (vesselInAnim && Player.gameObject.layer == PlayerLayer)
		{
			Player.gameObject.layer = GhostLayer;
		}
		else if (!vesselInAnim && Player.gameObject.layer == GhostLayer)
		{
			Player.gameObject.layer = PlayerLayer;
		}

		base.FixedUpdate();
	}

	public override void OnTimerComplete()
	{
		if (Player.AmOwner)
		{
			VesselRole.RpcGhostEndPossession(PlayerControl.LocalPlayer, Vessel);
		}
	}

	public override void CreateNotification()
	{
		if (Vessel == null || Player == null || !Player.AmOwner)
		{
			return;
		}

		if (notification == null)
		{
			var controllerText = TouLocale.GetParsed("PoltergeistControlNotif", $"You are possessing {Vessel.Data.PlayerName}!");
			notification = Helpers.CreateAndShowNotification(
				$"<b>{VesselRoleModColors.Vessel.ToTextColor()}{controllerText.Replace("<player>", Vessel.Data.PlayerName)}</color></b>",
				Color.white, new Vector3(0f, 2f, -20f), spr: VesselRoleIcons.Vessel.LoadAsset());
			notification?.AdjustNotification();
		}
	}
}
