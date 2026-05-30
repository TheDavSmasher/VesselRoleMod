using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.LocalSettings;
using MiraAPI.Modifiers;
using MiraAPI.PluginLoading;
using MiraAPI.Utilities;
using System.Globalization;
using TownOfUs;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules;
using TownOfUs.Options;
using TownOfUs.Utilities;
using UnityEngine;

namespace VesselRoleMod.Buttons;

[MiraIgnore]
public abstract class TouRoleTriggerButton<TRole> : TownOfUsRoleButton<TRole> where TRole : RoleBehaviour
{
	/// <summary>
	/// Similar to <see cref="CustomActionButton.HasEffect"/>
	/// </summary>
	public virtual bool HasTrigger => TriggerWindow > 0;
	/// <summary>
	/// Similar to <see cref="CustomActionButton.EffectDuration"/>
	/// </summary>
	public virtual float TriggerWindow => 0f;
	/// <summary>
	/// Similar to <see cref="CustomActionButton.EffectActive"/>
	/// </summary>
	public bool WaitingOnTrigger { get; set; }

	public virtual float MinDuration { get; set; }

	public virtual void EndTriggerWindow()
	{
		Timer = Cooldown;
		if (WaitingOnTrigger)
		{
			OnTriggerEnd();
		}

		WaitingOnTrigger = false;
		EffectActive = false;
	}

	public virtual void ActivateTriggerEffect()
	{
		Timer = EffectDuration;
		if (WaitingOnTrigger)
		{
			OnTriggerActivate();
		}

		WaitingOnTrigger = false;
		EffectActive = true;
	}

	public override void ResetCooldownAndOrEffect()
	{
		base.ResetCooldownAndOrEffect();
		WaitingOnTrigger = false;
	}

	public virtual void OnTriggerActivate()
	{
	}

	public virtual void OnTriggerEnd()
	{
	}

	public override void FixedUpdateHandler(PlayerControl playerControl)
	{
		if (Timer >= 0)
		{
			var shouldPauseInVent = ShouldPauseInVent && PlayerControl.LocalPlayer.inVent && !EffectActive;

			if (!TimerPaused && !OptionGroupSingleton<VanillaTweakOptions>.Instance.CanPauseCooldown && (!shouldPauseInVent || EffectActive))
			{
				Timer -= Time.deltaTime;
			}
		}
		else if (HasTrigger && WaitingOnTrigger)
		{
			WaitingOnTrigger = false;
			Timer = Cooldown;
			OnTriggerEnd();
		}
		else if (HasEffect && EffectActive)
		{
			EffectActive = false;
			Timer = Cooldown;
			OnEffectEnd();
		}

		if (Button)
		{
			if (CanUse())
			{
				Button!.SetEnabled();
			}
			else
			{
				Button!.SetDisabled();
			}

			if (WaitingOnTrigger)
			{
				Button.SetFillUp(TriggerWindow - Timer, TriggerWindow);

				Button.cooldownTimerText.text =
					Timer.ToString(CooldownTimerFormatString, NumberFormatInfo.InvariantInfo);
				Button.cooldownTimerText.gameObject.SetActive(true);
			}
			else if (EffectActive)
			{
				Button.SetFillUp(Timer, EffectDuration);

				Button.cooldownTimerText.text =
					Timer.ToString(CooldownTimerFormatString, NumberFormatInfo.InvariantInfo);
				Button.cooldownTimerText.gameObject.SetActive(true);
			}
			else
			{
				Button.SetCooldownFormat(Timer, Cooldown, CooldownTimerFormatString);
			}
		}

		FixedUpdate(playerControl);
	}

	public override void ClickHandler()
	{
		if (!CanClick() || PlayerControl.LocalPlayer.HasModifier<GlitchHackedModifier>() ||
			PlayerControl.LocalPlayer.HasModifier<DisabledModifier>(x => !x.CanUseAbilities))
		{
			return;
		}

		if (LimitedUses)
		{
			UsesLeft--;
			Button?.SetUsesRemaining(UsesLeft);
			TownOfUsColors.UseBasic = false;
			if (TextOutlineColor != Color.clear)
			{
				SetTextOutline(TextOutlineColor);
				if (Button != null)
				{
					Button.usesRemainingSprite.color = TextOutlineColor;
				}
			}

			TownOfUsColors.UseBasic = LocalSettingsTabSingleton<TownOfUsLocalRoleSettings>.Instance
				.UseCrewmateTeamColorToggle.Value;
		}

		OnClick();
		Button?.SetDisabled();

		if (HasTrigger && !WaitingOnTrigger)
		{
			WaitingOnTrigger = true;
			Timer = TriggerWindow;
		}
		else if (HasEffect && EffectActive)
		{
			EffectActive = false;
			Timer = Cooldown;
		}
		else
		{
			WaitingOnTrigger = false;
			Timer = Cooldown;
		}
	}

	public override bool CanClick()
	{
		if (!CanUse())
		{
			return false;
		}

		if (EffectActive)
		{
			return Timer <= EffectDuration - MinDuration;
		}
		else if (WaitingOnTrigger)
		{
			return Timer <= TriggerWindow - 2f;
		}
		else
		{
			return Timer <= 0;
		}
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

		if (PlayerControl.LocalPlayer.HasDied() && !UsableInDeath)
		{
			return false;
		}

		if (!PlayerControl.LocalPlayer.CanMove ||
			PlayerControl.LocalPlayer.HasModifier<DisabledModifier>(x => !x.CanUseAbilities))
		{
			return false;
		}

		return PlayerControl.LocalPlayer.moveable &&
			   (EffectActive || WaitingOnTrigger || !LimitedUses || UsesLeft > 0);
	}
}
