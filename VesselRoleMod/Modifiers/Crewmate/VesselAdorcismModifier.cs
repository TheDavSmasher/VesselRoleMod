using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers.Types;
using VesselRoleMod.Events;
using VesselRoleMod.Events.Crewmate;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Crewmate;

public sealed class VesselAdorcismModifier : TimedModifier
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;
	public override string ModifierName => "Vessel Adorcism";
	public override bool HideOnUi => true;

	private bool AdorcismProceeded;

	public override void OnActivate()
	{
		base.OnActivate();

		AdorcismProceeded = false;
		MiraEventManager.InvokeEvent(new CustomAbilityEvent<VesselAbilityType>(VesselAbilityType.AdorciseStart, Player));
	}

	public void OnAdorcismProceeding()
	{
		AdorcismProceeded = true;
		ModifierComponent?.RemoveModifier(this);
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();

		if (AdorcismProceeded)
		{
			// TODO: Procees with Adorcism
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
