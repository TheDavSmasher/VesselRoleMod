using MiraAPI.GameOptions;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class ValidAdorcismGhostModifier(PlayerControl vessel) : TimedModifier, IVesselSeekingModifier
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;
	public override string ModifierName => "ValidAdorcismGhost";
	public override bool Unique => false;
	public override bool HideOnUi => true;
	public PlayerControl Ghost => Player;
	public PlayerControl Vessel => vessel;


	private LobbyNotificationMessage? decisionNotification;

	public override void FixedUpdate()
	{
		TimerActive = true;
		if (VesselControlState.IsPausingTimer(Vessel.PlayerId))
		{
			TimerActive = false;
		}
		base.FixedUpdate();
	}

	public void CreateNotification()
	{
		if (Vessel == null || Player == null || !Player.AmOwner)
		{
			return;
		}

		if (decisionNotification == null)
		{
			var decisionText = TouLocale.GetParsed("VesselRoleVesselIsDeciding");
			decisionNotification = Helpers.CreateAndShowNotification(
				$"<b>{VesselRoleModColors.Vessel.ToTextColor()}{decisionText.Replace("<player>", Vessel.Data.PlayerName)}</color></b>",
				Color.white, new Vector3(0f, 2f, -20f), spr: VesselRoleIcons.Vessel.LoadAsset());
			decisionNotification?.AdjustNotification();
		}
	}

	public void ClearNotifications()
	{
		if (decisionNotification != null && decisionNotification.gameObject != null)
		{
			decisionNotification.gameObject.Destroy();
			decisionNotification = null;
		}
	}
}
