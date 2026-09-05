using MiraAPI.Keybinds;
using MiraAPI.Translation;
using MiraAPI.Utilities.Assets;
using TownOfUs.Buttons;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Buttons.Crewmate;

public sealed class VesselChangeControlButton : VesselRoleButton<IVesselPossessModifier>
{
	private static readonly string _ctrlTakeName = MiraLocaleManager.Get("VesselModTakeControl", "Take Control");
	private static readonly string _ctrlGiveName = MiraLocaleManager.Get("VesselModGiveControl", "Give Control");

	public override string Name => _ctrlGiveName;
	public override BaseKeybind Keybind => Keybinds.TertiaryAction;
	public override LoadableAsset<Sprite> Sprite => VesselCrewAssets.GhostControlSprite;

	protected override bool CanUseAbility()
	{
		return !VesselControlState.CanShareControl;
	}

	protected override void FixedUpdate(PlayerControl playerControl)
	{
		base.FixedUpdate(playerControl);

		if (VesselControlState.IsUsingState(PlayerControl.LocalPlayer.PlayerId, out _, out bool isVessel))
		{
			var hasControl = VesselControlState.HasControl(PlayerControl.LocalPlayer.PlayerId);
			var asset = hasControl == isVessel ? VesselCrewAssets.VesselControlSprite : VesselCrewAssets.GhostControlSprite;
			var name = hasControl ? _ctrlGiveName : _ctrlTakeName;

			OverrideSprite(asset.LoadAsset());
			OverrideName(name);
		}
	}

	public override bool CanUse()
	{
		if (Modifier?.Vessel != null && Modifier.Vessel.inVent)
		{
			return true;
		}

		return base.CanUse();
	}

	protected override void OnClick()
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		if (Modifier is not { } mod)
		{
			Error("ChangeControlButton - Invalid click source");
			return;
		}

		VesselRole.RpcChangeControl(mod.Ghost, mod.Vessel);
	}
}
