using MiraAPI.Modifiers.Types;
using MiraAPI.PluginLoading;

namespace VesselRoleMod.Modifiers;

[MiraIgnore]
public abstract class PossessionModifier : TimedModifier
{
	public override bool HideOnUi => true;

	public abstract PlayerControl Vessel { get; }

	public override void OnDeath(DeathReason reason)
	{
		ModifierComponent?.RemoveModifier(this);
	}

	public override void OnMeetingStart()
	{
		ModifierComponent?.RemoveModifier(this);
	}
}
