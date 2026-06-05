using MiraAPI.Events;
using MiraAPI.Hud;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs.Utilities.ControlSystem;
using VesselRoleMod.Assets;
using VesselRoleMod.Buttons.Crewmate;
using VesselRoleMod.Events;
using VesselRoleMod.Events.Crewmate;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Modifiers.Crewmate;

/// <summary>
/// Applied to the vessel while they are controlled by a Poltergeist.
/// Movement/input suppression is handled by Harmony patches while this modifier is present.
/// </summary>
public sealed class VesselPossessedModifier(PlayerControl ghost) : ActivePossessionModifier, IUncontrollable, ICachedRole
{
	public override string ModifierName => "Possessed";
	public override PlayerControl Target => Ghost;
	public override PlayerControl Vessel => Player;
	public override PlayerControl Ghost => ghost;

	public bool ShowCurrentRoleFirst => true;
	public bool Visible => true;
	public CacheRoleGuess GuessMode => CacheRoleGuess.ActiveRole;
	public RoleBehaviour CachedRole => Options.ReportGhostInstead
		? (this as IVesselModifier).Role
		: Player.Data.Role;

	public override void OnActivate()
	{
		base.OnActivate();

		if (Player.AmOwner)
		{
			CreateNotification();

			if (Minigame.Instance)
				Minigame.Instance.Close();

			if (MapBehaviour.Instance)
				MapBehaviour.Instance.Close();

			Player.MyPhysics.ClearVentState();

			Player.RemoveExistingModifier<VesselAdorcismModifier>();
		}

		var vesselAbilityEvent = new CustomAbilityEvent<VesselAbilityType>(VesselAbilityType.AdorcismSuccess, Ghost, Player);
		MiraEventManager.InvokeEvent(vesselAbilityEvent);
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();

		ClearNotification();

		var button = CustomButtonSingleton<VesselAdorciseButton>.Instance;
		if (Player.AmOwner && button != null && button.EffectActive)
		{
			button.ResetCooldownAndOrEffect();
		}
	}

	public override void CreateNotification()
	{
		if (Player == null || !Player.AmOwner || PlayerControl.LocalPlayer == null)
		{
			return;
		}

		if (notification == null)
		{
			var ghostName = (Options.CanRejectPossession == VesselRejectionType.Free || Options.CanSeeGhostName) ? Ghost.Data.PlayerName :
				Ghost?.Data?.Role is ITownOfUsRole touRole ? touRole.RoleName : "Poltergeist";
			notification = ControlledFeedbackUtilities.ShowControlledByNotification(
				ghostName,
				VesselRoleModColors.Vessel,
				VesselRoleIcons.Vessel.LoadAsset());
			notification?.AdjustNotification();
		}
	}
}
