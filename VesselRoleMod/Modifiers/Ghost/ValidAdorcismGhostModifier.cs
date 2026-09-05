using MiraAPI.Translation;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Assets;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class ValidAdorcismGhostModifier(PlayerControl vessel) : OpenAdorcismModifier, IVesselSeekingModifier
{
	public override string ModifierName => "ValidAdorcismGhost";
	public override bool Unique => false;
	public PlayerControl Ghost => Player;
	public override PlayerControl Vessel => vessel;

	private LobbyNotificationMessage? decisionNotification;

	public void CreateNotification()
	{
		if (Vessel == null || Player == null || !Player.AmOwner)
		{
			return;
		}

		if (decisionNotification == null)
		{
			var decisionText = MiraLocaleManager.Get("VesselRoleVesselIsDeciding");
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
