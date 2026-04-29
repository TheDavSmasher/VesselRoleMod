using MiraAPI.Modifiers.Types;
using MiraAPI.PluginLoading;

namespace VesselRoleMod.Modifiers.Ghost;

[MiraIgnore]
public abstract class VesselSeekingModifier(PlayerControl vessel) : TimedModifier
{
	public override bool HideOnUi => true;

	public PlayerControl Vessel => vessel;
}
