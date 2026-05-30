using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Usables;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Buttons.Crewmate;
using UnityEngine;
using VesselRoleMod.Modifiers;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistVentButton : PoltergeistTargetButton<IVesselPossessModifier, Vent>
{
	public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.VentLabel, "Vent");
	public override BaseKeybind Keybind => Keybinds.VentAction;
	public override float Cooldown => Modifier?.Role is EngineerRole && (HasEffect || Modifier.Vessel != null && Modifier.Vessel.inVent)
		? CustomButtonSingleton<EngineerVentButton>.Instance.Cooldown
		: base.Cooldown;
	public override float EffectDuration => Modifier?.Role is EngineerRole
		? CustomButtonSingleton<EngineerVentButton>.Instance.EffectDuration
		: base.EffectDuration;
	public override LoadableAsset<Sprite> Sprite => TouCrewAssets.EngiVentSprite;

	protected override bool CanUseAbility()
	{
		return VesselControlState.IsUsingState(PlayerControl.LocalPlayer.PlayerId, out _, out _) &&
			   OptionGroupSingleton<VesselOptions>.Instance.VentingGhostsCanVent &&
			   Modifier!.Role.HasVentingAbility();
	}

	public override Vent? GetTarget()
	{
		if (Modifier?.Vessel == null || Modifier.Vessel.Data.IsDead)
		{
			return null;
		}

		if (Modifier.Vessel.inVent)
		{
			return Vent.currentVent;
		}

		return DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.currentTarget;
	}

	public override bool CanUse()
	{
		return base.CanUse() || Modifier!.Vessel.inVent;
	}

	protected override void OnClick()
	{
		if (Modifier?.Vessel == null || Modifier.Vessel.walkingToVent)
		{
			return;
		}

		if (!Modifier.Vessel.inVent)
		{
			if (Target != null)
			{
				Modifier.Vessel.MyPhysics.RpcEnterVent(Target.Id);
				Target.SetButtons(true);
			}
		}
		else if (!HasEffect || Timer > 0)
		{
			OnEffectEnd();
			if (!HasEffect)
			{
				EffectActive = false;
				Timer = Cooldown;
			}
		}
	}

	public override void OnEffectEnd()
	{
		if (Modifier?.Vessel == null || !Modifier.Vessel.inVent)
		{
			return;
		}

		_ = Vent.currentVent.CanUse(PlayerControl.LocalPlayer.Data, out _, out var couldUse);
		Vent.currentVent.SetButtons(false);

		if (!couldUse)
		{
			Error($"Current vent {Vent.currentVent.name} ({Vent.currentVent.Id}) cannot be exited, finding alternate route.");
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
				Modifier.Vessel.MyPhysics.RpcExitVent(newVent.Id);
				return;
			}
		}

		Modifier.Vessel.MyPhysics.RpcExitVent(Vent.currentVent.Id);
	}
}
