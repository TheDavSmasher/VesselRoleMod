using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TownOfUs.Events.TouEvents;
using TownOfUs.Utilities;
using VesselRoleMod.Modifiers.Crewmate;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Events;

public static class VesselModEvents
{
	[RegisterEvent]
	public static void UpdateButtonUses(RoundStartEvent @event)
	{
		if (!@event.TriggeredByIntro)
		{
			return;
		}

		foreach (var button in CustomButtonManager.Buttons)
		{
			if (button?.Button == null)
			{
				continue;
			}

			button.Button.name = button.Name + "Button";
			button.Button.OverrideText(button.Name.ToUpperInvariant());

			button.Button.graphic.sprite = button.Sprite.LoadAsset();
			button.Button.SetUsesRemaining(button.MaxUses);
			if (button.MaxUses <= 0)
			{
				button.Button.SetInfiniteUses();
			}
		}
	}

	[RegisterEvent]
	public static void ChangeRoleHandler(ChangeRoleEvent @event)
	{
		var player = @event.Player;

		if (!PlayerControl.LocalPlayer || player == null)
		{
			return;
		}

		if (player.Data.Role is VesselRole && player.TryGetModifier<VesselPossessedModifier>(out var mod))
		{
			VesselRole.RpcGhostEndPossession(mod.Ghost, player);
		}

		if (VesselControlState.IsControlled(player.PlayerId, out var controllerId))
		{
			var controller = MiscUtils.PlayerById(controllerId);
			if (controller != null && controller.TryGetModifier<PoltergeistModifier>(out var ghostMod) && ghostMod.Vessel == player)
			{
				VesselRole.RpcGhostEndPossession(controller, player);
			}
			else
			{
				VesselControlState.ClearControl(player.PlayerId);
			}
		}
	}
}
