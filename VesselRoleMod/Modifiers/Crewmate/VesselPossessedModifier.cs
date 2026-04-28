using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using VesselRoleMod.Events;
using VesselRoleMod.Events.Crewmate;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Crewmate;

public sealed class VesselPossessedModifier(PlayerControl ghost) : TimedModifier
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;
	public override string ModifierName => "Possessed";
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
