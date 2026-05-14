using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs.Utilities.ControlSystem;
using VesselRoleMod.Assets;
using VesselRoleMod.Buttons.Crewmate;
using VesselRoleMod.Events;
using VesselRoleMod.Events.Crewmate;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Crewmate;

/// <summary>
/// Applied to the vessel while they are controlled by a Poltergeist.
/// Movement/input suppression is handled by Harmony patches while this modifier is present.
/// </summary>
public sealed class VesselPossessedModifier(PlayerControl ghost) : DisabledModifier, IUncontrollable, IVesselModifier
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;
	public override string ModifierName => "Possessed";
	public override bool CanUseAbilities => true;
	public override bool CanReport => true;
	public override bool HideOnUi => true;
	public override bool AutoStart => true;
	public PlayerControl Target => Ghost;
	public PlayerControl Vessel => Player;
	public PlayerControl Ghost => ghost;

	private LobbyNotificationMessage? _possessedNotification;

	public override void OnActivate()
	{
		if (Player.AmOwner)
		{
			CreateNotification();

			if (Minigame.Instance)
				Minigame.Instance.Close();

			if (MapBehaviour.Instance)
				MapBehaviour.Instance.Close();
			if (Player.inVent)
			{
				Player.MyPhysics.RpcExitVent(Vent.currentVent.Id);
				Player.MyPhysics.ExitAllVents();
			}

			if (Player.HasModifier<VesselAdorcismModifier>())
			{
				Player.RemoveModifier<VesselAdorcismModifier>();
			}
		}

		var vesselAbilityEvent = new CustomAbilityEvent<VesselAbilityType>(VesselAbilityType.AdorcismSuccess, Ghost, Player);
		MiraEventManager.InvokeEvent(vesselAbilityEvent);
	}

	public override void OnDeactivate()
	{
		ClearNotification();
		if (Player.AmOwner)
		{
			var button = CustomButtonSingleton<VesselAdorciseButton>.Instance;

			if (button != null && button.EffectActive)
			{
				button.ResetCooldownAndOrEffect();
			}
		}
	}

	public override void OnDeath(DeathReason reason)
	{
		ModifierComponent?.RemoveModifier(this);
	}

	public override void OnMeetingStart()
	{
		ModifierComponent?.RemoveModifier(this);
	}

	private void CreateNotification()
	{
		if (Player == null || !Player.AmOwner || PlayerControl.LocalPlayer == null)
		{
			return;
		}

		if (_possessedNotification == null)
		{
			var ghostName = Ghost?.Data?.Role is ITownOfUsRole touRole ? touRole.RoleName : "Poltergeist";
			_possessedNotification = ControlledFeedbackUtilities.ShowControlledByNotification(
				ghostName,
				VesselRoleModColors.Vessel,
				VesselRoleIcons.Vessel.LoadAsset());
			_possessedNotification?.AdjustNotification();
		}
	}

	public void ClearNotification()
	{
		ControlledFeedbackUtilities.ClearNotification(ref _possessedNotification);
	}
}
