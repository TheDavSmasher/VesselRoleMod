using MiraAPI.Events;
using MiraAPI.Modifiers;
using VesselRoleMod.Events;
using VesselRoleMod.Events.Crewmate;

namespace VesselRoleMod.Modifiers.Crewmate;

public sealed class VesselPossessedModifier : BaseModifier
{
	public override string ModifierName => "Possessed";

	public override void OnActivate()
	{
		base.OnActivate();

		if (Player.HasModifier<VesselAdorcismModifier>())
		{
			Player.RpcRemoveModifier<VesselAdorcismModifier>();
		}

		MiraEventManager.InvokeEvent(new CustomAbilityEvent<VesselAbilityType>(VesselAbilityType.AdorcismSuccess, Player));
	}
}
