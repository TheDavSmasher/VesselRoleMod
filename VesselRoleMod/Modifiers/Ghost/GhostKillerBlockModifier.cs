using MiraAPI.Modifiers;


namespace VesselRoleMod.Modifiers.Ghost;

public sealed class GhostKillerBlockModifier : BaseModifier
{
	public override string ModifierName => "Ghost Blocked";
	public override bool HideOnUi => true;
}
