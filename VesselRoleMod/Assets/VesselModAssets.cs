using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace VesselRoleMod.Assets;

public static class VesselModAssets
{
	public static LoadableAsset<Sprite> VesselBlockedSprite { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.VesselBlocked.png");

	public static LoadableAsset<Sprite> VesselUnblockedSprite { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.VesselUnblocked.png");
}
