using MiraAPI.Modifiers;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Crewmate;

public sealed class GhostPossessorModifier(PlayerControl vessel) : BaseModifier
{
	public override string ModifierName => "Ghost Possessor";

	public override bool HideOnUi => true;

	public PlayerControl Vessel => vessel;

	public bool IsKillingGhost()
	{
		return vessel.Data.Role is VesselRole;
	}
}
