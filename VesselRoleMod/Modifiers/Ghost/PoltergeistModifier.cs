using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Buttons.Modifiers;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class PoltergeistModifier(PlayerControl vessel) : VesselSeekingModifier(vessel), IVisualAppearance
{
	public override string ModifierName => "Ghost Possessor";

	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;

	private LobbyNotificationMessage? controllerNotification;

	public VisualAppearance? GetVisualAppearance()
	{
		var vesselAppearance = Vessel.GetDefaultAppearance();
		var appearance = Player.GetDefaultAppearance();
		appearance.Speed = vesselAppearance.Speed;
		return appearance;
	}

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

		Player.RawSetAppearance(this);
	}

	public override void OnDeactivate()
	{
		if (!Player.AmOwner)
		{
			return;
		}

		Player.ResetAppearance();

		var button = CustomButtonSingleton<PoltergeistPossessButton>.Instance;

		if (button != null && button.EffectActive)
		{
			button.ResetCooldownAndOrEffect();
		}
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

	public void ClearNotifications()
	{
		if (controllerNotification != null && controllerNotification.gameObject != null)
		{
			controllerNotification.gameObject.Destroy();
			controllerNotification = null;
		}
	}
}
