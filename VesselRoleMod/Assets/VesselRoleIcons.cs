using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace VesselRoleMod.Assets;

public static class VesselRoleIcons
{
	public static LoadableAsset<Sprite> Vessel { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Vessel_Role_Icon.png");
}
