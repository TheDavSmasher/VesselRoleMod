using MiraAPI.GameOptions;
using MiraAPI.Hud;
using VesselRoleMod.Buttons.Modifiers;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class ValidAdorcismGhostModifier(PlayerControl vessel) : VesselSeekingModifier(vessel)
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;
	public override string ModifierName => "ValidAdorcismGhost";
	public override bool Unique => false;

	public override void OnActivate()
	{
		CustomButtonSingleton<PoltergeistPossessButton>.Instance.SetActive(true, Player.Data.Role);
	}

	public override void OnDeactivate()
	{
		CustomButtonSingleton<PoltergeistPossessButton>.Instance.SetActive(false, Player.Data.Role);
	}
}
