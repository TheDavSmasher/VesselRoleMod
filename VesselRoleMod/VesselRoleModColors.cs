using TownOfUs;
using UnityEngine;

namespace VesselRoleMod;

public static class VesselRoleModColors
{
	public static Color Vessel => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(46, 16, 143, 255);
}
