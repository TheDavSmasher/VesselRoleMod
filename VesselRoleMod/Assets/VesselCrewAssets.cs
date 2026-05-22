using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace VesselRoleMod.Assets;

public static class VesselCrewAssets
{
	public static LoadableAsset<Sprite> AdorciseSprite { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Vessel_Adorcise_Button.png", 440f);

	public static LoadableAsset<Sprite> ExorciseSprite { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Vessel_Exorcise_Button.png", 440f);

	public static LoadableAsset<Sprite> PossessButton { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Vessel_Possess_Button.png", 440f);

	public static LoadableAsset<Sprite> ReleaseButton { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Vessel_Release_Button.png", 440f);

	public static LoadableAsset<Sprite> VesselControlSprite { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Vessel_Control_Button.png", 600f);

	public static LoadableAsset<Sprite> GhostControlSprite { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Ghost_Control_Button.png", 600f);
}
