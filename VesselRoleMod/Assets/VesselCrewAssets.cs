using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace VesselRoleMod.Assets;

public static class VesselCrewAssets
{
	public static LoadableAsset<Sprite> AdorciseSprite { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Vessel_Adorcise_Button.png");

	public static LoadableAsset<Sprite> ExorciseSprite { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Vessel_Exorcise_Button.png");

	public static LoadableAsset<Sprite> PossessButton { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Vessel_Possess_Button.png");

	public static LoadableAsset<Sprite> ReleaseButton { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Vessel_Release_Button.png");

	public static LoadableAsset<Sprite> TakeControlSprite { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Vessel_Take_Control_Button.png");

	public static LoadableAsset<Sprite> GiveControlSprite { get; } =
		new LoadableResourceAsset("VesselRoleMod.Resources.Buttons.Vessel_Give_Control_Button.png");
}
