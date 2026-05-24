using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Usables;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Buttons.Crewmate;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistVentButton : PoltergeistTargetButton<PoltergeistModifier, Vent>
{
	public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.VentLabel, "Vent");
	public override BaseKeybind Keybind => Keybinds.VentAction;
	public override float Cooldown => Role is EngineerRole && Vessel != null && !Vessel.inVent
		? CustomButtonSingleton<EngineerVentButton>.Instance.Cooldown
		: base.Cooldown;
	public override float EffectDuration => Role is EngineerRole
		? CustomButtonSingleton<EngineerVentButton>.Instance.EffectDuration
		: base.EffectDuration;
	public override LoadableAsset<Sprite> Sprite => TouCrewAssets.EngiVentSprite;

	protected override bool CanUseAbility()
	{
		return Modifier!.CanVent() == true;
	}

	public override Vent? GetTarget()
	{
		return Vessel?.GetClosestUsableVent(true);
	}

	public override bool CanUse()
	{
		return base.CanUse() || Vessel!.inVent;
	}

	protected override void OnClick()
	{
		if (Vessel != null && Vessel.inVent)
		{
			if (Target != null)
			{
				Vessel.MyPhysics.RpcEnterVent(Target.Id);
				if (Role is not JesterRole)
				{
					Target.SetButtons(true);
				}
				if (Role is MinerRole)
				{
					HandleMinerVents();
				}
			}
		}
		else if (Timer != 0)
		{
			OnEffectEnd();
			if (!HasEffect)
			{
				EffectActive = false;
				Timer = Cooldown;
			}
		}
	}

	private void HandleMinerVents()
	{
		if (Target == null || !Target.name.Contains("MinerVent"))
		{
			return;
		}

		Vent[] nearbyVents = Target.NearbyVents;
		for (var i = 0; i < Target.Buttons.Length; i++)
		{
			var buttonBehavior = Target.Buttons[i];
			var vent = nearbyVents[i];

			if (vent != null && !vent.myRend.enabled)
			{
				buttonBehavior.gameObject.SetActive(false);
			}
		}
	}

	public override void OnEffectEnd()
	{
		if (Vessel == null || !Vessel.inVent)
		{
			return;
		}

		_ = Vent.currentVent.CanUse(Vessel, true, out var couldUse);
		Vent.currentVent.SetButtons(false);

		if (!couldUse)
		{
			Error($"Current vent cannot be exited, finding alternate route.");
			Vent? newVent = null;
			foreach (var closeVent in Vent.currentVent.NearbyVents)
			{
				if (newVent != null)
				{
					break;
				}
				var @event = new PlayerCanUseEvent(closeVent.Cast<IUsable>());
				MiraEventManager.InvokeEvent(@event);

				if (!@event.IsCancelled)
				{
					newVent = closeVent;
				}
			}

			if (newVent != null)
			{
				Vessel.MyPhysics.RpcExitVent(newVent.Id);
				return;
			}
		}

		Vessel.MyPhysics.RpcExitVent(Vent.currentVent.Id);
	}
}
