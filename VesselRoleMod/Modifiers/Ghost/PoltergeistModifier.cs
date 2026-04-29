using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using Reactor.Networking.Attributes;
using TownOfUs.Utilities;
using VesselRoleMod.Buttons.Modifiers;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class PoltergeistModifier(PlayerControl vessel) : VesselSeekingModifier(vessel)
{
	public override string ModifierName => "Ghost Possessor";

	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;

	public bool CanKill()
	{
		return Vessel.Data.Role is VesselRole;
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();

		var button = CustomButtonSingleton<PoltergeistPossessButton>.Instance;

		if (button != null && button.EffectActive)
		{
			button.ResetCooldownAndOrEffect();
		}
	}
}
