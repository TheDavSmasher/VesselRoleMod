using MiraAPI.GameOptions;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class ValidAdorcismGhostModifier(PlayerControl vessel) : VesselSeekingModifier(vessel)
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;
	public override string ModifierName => "ValidAdorcismGhost";
	public override bool Unique => false;
}
