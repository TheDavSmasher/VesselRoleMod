using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TownOfUs.Assets;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Events;
using VesselRoleMod.Events.Crewmate;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Crewmate;

public sealed class VesselPossessedModifier(PlayerControl ghost) : DisabledModifier
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;
	public override string ModifierName => "Possessed";
	public override bool CanUseAbilities => true;
	public override bool CanReport => true;
	public override bool HideOnUi => true;
	public PlayerControl Ghost => ghost;

	public override void OnActivate()
	{
		base.OnActivate();

		if (Player.HasModifier<VesselAdorcismModifier>())
		{
			Player.RpcRemoveModifier<VesselAdorcismModifier>();
		}

		MiraEventManager.InvokeEvent(new CustomAbilityEvent<VesselAbilityType>(VesselAbilityType.AdorcismSuccess, Ghost, Player));

		var notif1 = Helpers.CreateAndShowNotification(
			"Adorcism Succeeded with Possession",
			Color.white, new Vector3(0f, 1f, -20f), spr: TouRoleIcons.Medium.LoadAsset());
		notif1.AdjustNotification();
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();
	}

	public override void OnDeath(DeathReason reason)
	{
		ModifierComponent?.RemoveModifier(this);
	}

	public override void OnMeetingStart()
	{
		ModifierComponent?.RemoveModifier(this);
	}
}
