using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using System.Linq;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using UnityEngine;
using VesselRoleMod.Assets;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

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
	public override LoadableAsset<Sprite> Sprite => VesselCrewAssets.GhostControlSprite;

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

	public override bool Enabled(RoleBehaviour? role)
	{
		return !VesselControlState.CanShareControl &&
			PlayerControl.LocalPlayer != null && role != null && 
			role.Player.HasModifierOfType<IVesselModifier>();
	}

	public override bool CanUse()
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return false;
		}

		if (TimeLordRewindSystem.IsRewinding)
		{
			return false;
		}

		if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
		{
			return false;
		}

		if (PlayerControl.LocalPlayer.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
		{
			return false;
		}

		return PlayerControl.LocalPlayer.moveable || PlayerControl.LocalPlayer.inVent;
	}

	protected override void OnClick()
	{
		if (PlayerControl.LocalPlayer == null)
		{
			return;
		}

		if (PlayerControl.LocalPlayer.GetModifierOfType<IVesselModifier>() is not { } mod)
		{
			Error("ChangeControlButton - Invalid click source");
			return;
		}

		VesselRole.RpcChangeControl(mod.Ghost, mod.Vessel);
	}
}
