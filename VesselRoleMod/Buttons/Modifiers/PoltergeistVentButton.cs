using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Usables;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using System.Linq;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Buttons.Crewmate;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;
using VesselRoleMod.Modifiers.Ghost;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Buttons.Modifiers;

public sealed class PoltergeistVentButton : TownOfUsTargetButton<Vent>
{
	public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.VentLabel, "Vent");
	public override bool UsableInDeath => true;
	public override BaseKeybind Keybind => Keybinds.VentAction;
	public override float InitialCooldown => 0.001f;
	public override float Cooldown => Role is EngineerRole
		? CustomButtonSingleton<EngineerVentButton>.Instance.Cooldown : 0.001f;
	public override float EffectDuration => Role is EngineerRole
		? CustomButtonSingleton<EngineerVentButton>.Instance.EffectDuration : 0f;
	public override LoadableAsset<Sprite> Sprite => TouAssets.VentSprite;
	public override Color TextOutlineColor => VesselRoleModColors.Vessel;

	private static RoleBehaviour Role => PlayerControl.LocalPlayer.GetRoleWhenAlive();
	private static PlayerControl? Vessel => PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>()?.Vessel;

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   PlayerControl.LocalPlayer.Data.IsDead &&
			   PlayerControl.LocalPlayer.TryGetModifier<PoltergeistModifier>(out var mod) &&
			   mod.CanVent() == true;
	}

	public override Vent? GetTarget()
	{
		return Vessel?.GetClosestUsableVent(true);
	}

	public override void SetOutline(bool active)
	{
		if (Target != null && PlayerControl.LocalPlayer.HasDied())
		{
			Target.SetOutline(active, true, VesselRoleModColors.Vessel);
		}
	}

	public override bool CanUse()
	{
		var newTarget = GetTarget();
		if (newTarget != Target)
		{
			Target?.SetOutline(false, false);
		}

		Target = IsTargetValid(newTarget) ? newTarget : null;
		SetOutline(true);

		if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
		{
			return false;
		}

		if (PlayerControl.LocalPlayer.HasModifier<GlitchHackedModifier>() || PlayerControl.LocalPlayer
				.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
		{
			return false;
		}

		if (Vessel == null)
		{
			return false;
		}

		return (Timer <= 0 && !Vessel.inVent && Target != null) || Vessel.inVent;
	}

	public override void ClickHandler()
	{
		if (!CanUse())
		{
			return;
		}

		OnClick();
		Button?.SetDisabled();
		if (EffectActive)
		{
			Timer = Cooldown;
			EffectActive = false;
		}
		else if (HasEffect)
		{
			EffectActive = true;
			Timer = EffectDuration;
		}
		else
		{
			Timer = !Vessel!.inVent ? 0.001f : Cooldown;
		}
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

		_ = Vent.currentVent.CanUse(Vessel.Data, true, out var couldUse);
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
