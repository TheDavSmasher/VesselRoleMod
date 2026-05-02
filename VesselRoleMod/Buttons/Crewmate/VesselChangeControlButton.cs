using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Buttons.Crewmate;

public sealed class VesselChangeControlButton : TownOfUsButton
{
	private static readonly string _ctrlTakeName = TouLocale.GetParsed("VesselModTakeControl", "Take Control");
	private static readonly string _ctrlGiveName = TouLocale.GetParsed("VesselModGiveControl", "Give Control");

	public override string Name => _ctrlGiveName;
	public override bool UsableInDeath => true;
	public override BaseKeybind Keybind => Keybinds.TertiaryAction;
	public override float InitialCooldown => 0.01f;
	public override float Cooldown => 0.01f;
	public override LoadableAsset<Sprite> Sprite => VesselCrewAssets.GiveControlSprite;

	protected override void FixedUpdate(PlayerControl playerControl)
	{
		base.FixedUpdate(playerControl);

		if (VesselControlState.IsUsingState(PlayerControl.LocalPlayer.PlayerId, out var toggleWithId))
		{
			var hasControl = VesselControlState.HasControlOver(PlayerControl.LocalPlayer.PlayerId, toggleWithId);
			var asset = hasControl ? VesselCrewAssets.GiveControlSprite : VesselCrewAssets.TakeControlSprite;
			var name = hasControl ? _ctrlGiveName : _ctrlTakeName;

			OverrideSprite(asset.LoadAsset());
			OverrideName(name);
		}
	}

	public override bool Enabled(RoleBehaviour? role)
	{
		return VesselControlState.CanShareControl &&
			PlayerControl.LocalPlayer != null && role != null && 
			((role is VesselRole vessel && vessel.Ghost != null) ||
			 role.Player.HasModifier<PoltergeistModifier>());
	}

	protected override void OnClick()
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		PlayerControl ghost, vessel;

		if (PlayerControl.LocalPlayer.Data.Role is VesselRole role && role.Ghost != null)
		{
			ghost = role.Ghost;
			vessel = PlayerControl.LocalPlayer;
		}
		else if (PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>() is { } mod && mod.Vessel != null)
		{
			ghost = PlayerControl.LocalPlayer;
			vessel = mod.Vessel;
		}
		else
		{
			Error("ChangeControlButton - Invalid click source");
			return;
		}

		VesselRole.RpcChangeControl(ghost, vessel);
	}
}
