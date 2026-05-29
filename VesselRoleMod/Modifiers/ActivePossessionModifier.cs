using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.PluginLoading;
using Reactor.Utilities.Extensions;
using VesselRoleMod.Buttons;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers;

[MiraIgnore]
public abstract class ActivePossessionModifier<TButton> : PossessionModifier, IVesselPossessModifier where TButton : CustomActionButton, IPossessionButton
{
	public override bool HideOnUi => true;
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;

	public abstract PlayerControl Target { get; }
	public abstract PlayerControl Ghost { get; }

	protected TButton Button => CustomButtonSingleton<TButton>.Instance;

	protected LobbyNotificationMessage? notification;

	public override void OnActivate()
	{
		if (!Player.AmOwner)
		{
			return;
		}

		if (Button != null && !Button.EffectActive)
		{
			Button.OnSuccess();
		}
	}

	public override void OnDeactivate()
	{
		if (!Player.AmOwner)
		{
			return;
		}

		if (Button != null && Button.EffectActive)
		{
			Button.ResetCooldownAndOrEffect();
		}
	}

	public abstract void CreateNotification();

	public void ClearNotification()
	{
		if (notification != null && notification.gameObject != null)
		{
			notification.gameObject.Destroy();
			notification = null;
		}
	}
}
