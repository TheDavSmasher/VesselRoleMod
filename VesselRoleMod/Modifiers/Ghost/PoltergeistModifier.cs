using MiraAPI.GameOptions;
using MiraAPI.Hud;
using TownOfUs.Modules;
using VesselRoleMod.Buttons.Modifiers;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class PoltergeistModifier(PlayerControl vessel) : VesselSeekingModifier(vessel)
{
	public override string ModifierName => "Ghost Possessor";

	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;

	public bool CanKill()
	{
		return OptionGroupSingleton<VesselOptions>.Instance.KillingGhostsCanKill && 
			   Vessel.Data.Role is VesselRole && 
			   Player.GetRoleWhenAlive().HasKillingAbility();
	}

	public override void OnDeactivate()
	{
		if (!Player.AmOwner)
		{
			return;
		}

		var button = CustomButtonSingleton<PoltergeistPossessButton>.Instance;

		if (button != null && button.EffectActive)
		{
			button.ResetCooldownAndOrEffect();
		}
	}
}
