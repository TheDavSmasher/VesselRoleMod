using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using VesselRoleMod.Events;
using VesselRoleMod.Events.Crewmate;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Crewmate;

public sealed class VesselAdorcismModifier : TimedModifier
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;
	public override string ModifierName => "Vessel Adorcism";
	public override bool HideOnUi => true;

	public override void OnActivate()
	{
		base.OnActivate();

		var vesselAbilityEvent = new CustomAbilityEvent<VesselAbilityType>(VesselAbilityType.AdorciseStart, Player);
		MiraEventManager.InvokeEvent(vesselAbilityEvent);
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();

		if (!Player.HasModifier<VesselPossessedModifier>())
		{
			var vesselAbilityEvent = new CustomAbilityEvent<VesselAbilityType>(VesselAbilityType.AdorciseEnd, Player);
			MiraEventManager.InvokeEvent(vesselAbilityEvent);
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
}
