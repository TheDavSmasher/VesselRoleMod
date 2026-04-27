using MiraAPI.Utilities.Assets;
using TownOfUs.Assets;
using UnityEngine;

namespace VesselRoleMod.Assets;

public static class VesselModAssets
{
	public static LoadableAsset<Sprite> VesselBlockedSprite { get; } = TouAssets.ImitateDeselectSprite;

	public static LoadableAsset<Sprite> VesselUnblockedSprite { get; } = TouAssets.ImitateSelectSprite;
}
