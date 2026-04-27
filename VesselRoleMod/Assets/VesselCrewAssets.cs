using MiraAPI.Utilities.Assets;
using TownOfUs.Assets;
using UnityEngine;

namespace VesselRoleMod.Assets;

public static class VesselCrewAssets
{
	public static LoadableAsset<Sprite> AdorciseSprite { get; } = TouCrewAssets.MediateSprite;

	public static LoadableAsset<Sprite> ExorciseSprite { get; } = TouCrewAssets.MediateSprite;
}
