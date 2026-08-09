using MiraAPI.Hud;
using MiraAPI.Modifiers.Types;
using VesselRoleMod.Buttons.Modifiers;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class GhostVentCooldownModifier : TimedModifier
{
	public override float Duration => CustomButtonSingleton<PoltergeistVentButton>.Instance.Cooldown;

	public override string ModifierName => "Vent Cooldown";

	public override void OnDeath(DeathReason reason)
	{
		this.RemoveSelf();
	}

	public override void OnMeetingStart()
	{
		this.RemoveSelf();
	}
}
