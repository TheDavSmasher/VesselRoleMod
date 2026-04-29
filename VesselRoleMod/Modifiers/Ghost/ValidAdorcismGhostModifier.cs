using MiraAPI.GameOptions;
using MiraAPI.Modifiers.Types;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class ValidAdorcismGhostModifier(PlayerControl vessel) : TimedModifier
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;
	public override string ModifierName => "ValidAdorcismGhost";

	public PlayerControl Vessel => vessel;
}
