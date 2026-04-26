using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace VesselRoleMod.Assets;

public static class VesselModAssets
{
	public static LoadableAsset<Sprite> VesselBlockedSprite { get; } = null;

	public static LoadableAsset<Sprite> VesselUnblockedSprite { get; } = null;
}
