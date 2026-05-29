using MiraAPI.Modifiers;

namespace VesselRoleMod.Modifiers;

public class TestCanVentModifier : BaseModifier
{
	public override string ModifierName => "Test CanVent";

	public override bool? CanVent()
	{
		return true;
	}

	public override string GetDescription()
	{
		return "\"You can vent.\" -MiraAPI";
	}
}
