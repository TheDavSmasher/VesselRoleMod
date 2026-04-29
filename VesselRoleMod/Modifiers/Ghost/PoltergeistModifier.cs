using MiraAPI.Modifiers;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Ghost;

public sealed class PoltergeistModifier(PlayerControl vessel) : BaseModifier
{
	public override string ModifierName => "Ghost Possessor";

	public override bool HideOnUi => true;

	public PlayerControl Vessel => vessel;

	public bool CanKill()
	{
		return Vessel.Data.Role is VesselRole;
	}
}
