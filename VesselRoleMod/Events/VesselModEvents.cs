using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Hud;

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
}
