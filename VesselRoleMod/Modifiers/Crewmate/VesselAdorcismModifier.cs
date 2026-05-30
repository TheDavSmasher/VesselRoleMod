using MiraAPI.Events;
using MiraAPI.Modifiers;
using VesselRoleMod.Events;
using VesselRoleMod.Events.Crewmate;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Crewmate;

public sealed class VesselAdorcismModifier : OpenAdorcismModifier
{
	public override string ModifierName => "Vessel Adorcism";

	public override void OnActivate()
	{
		base.OnActivate();

		VesselControlState.SetTimerActive(Player.PlayerId);

		var vesselAbilityEvent = new CustomAbilityEvent<VesselAbilityType>(VesselAbilityType.AdorciseStart, Player);
		MiraEventManager.InvokeEvent(vesselAbilityEvent);
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();

		VesselControlState.ClearTimer(Player.PlayerId);

		if (!Player.HasModifier<VesselPossessedModifier>())
		{
			var vesselAbilityEvent = new CustomAbilityEvent<VesselAbilityType>(VesselAbilityType.AdorciseEnd, Player);
			MiraEventManager.InvokeEvent(vesselAbilityEvent);
		}

		VesselRole.VesselClosed(Player);
	}
}
