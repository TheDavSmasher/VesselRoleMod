using MiraAPI.GameOptions;
using MiraAPI.Modifiers.Types;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Crewmate;

public sealed class VesselAdorcismModifier : TimedModifier
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;

	public override string ModifierName => "Vessel Adorcism";
}
