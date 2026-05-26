using MiraAPI.Modifiers;


namespace VesselRoleMod.Modifiers.Ghost;

public sealed class GhostKillerBlockModifier(bool vesselOwner) : BaseModifier
{
	public override string ModifierName => "Ghost Blocked";
	public override bool HideOnUi => true;
	public bool VesselOwner => vesselOwner;
}
