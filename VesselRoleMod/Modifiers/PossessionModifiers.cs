using MiraAPI.GameOptions;
using MiraAPI.Modifiers.Types;
using MiraAPI.PluginLoading;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;
using VesselRoleMod.Utilities;

namespace VesselRoleMod.Modifiers;

[MiraIgnore]
public abstract class PossessionModifier : TimedModifier
{
	public override bool HideOnUi => true;

	public abstract PlayerControl Vessel { get; }

	public override void OnDeath(DeathReason reason)
	{
		this.RemoveSelf();
	}

	public override void OnMeetingStart()
	{
		this.RemoveSelf();
	}
}

[MiraIgnore]
public abstract class OpenAdorcismModifier : PossessionModifier
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;
	public override bool HideOnUi => true;
	public override PlayerControl Vessel => Player;

	public override void FixedUpdate()
	{
		TimerActive = true;
		if (VesselControlState.IsPausingTimer(Vessel.PlayerId))
		{
			TimerActive = false;
		}

		base.FixedUpdate();
	}
}

[MiraIgnore]
public abstract class ActivePossessionModifier : PossessionModifier, IVesselPossessModifier
{
	public override bool HideOnUi => true;
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.PossessionDuration;

	public abstract PlayerControl Target { get; }
	public abstract PlayerControl Ghost { get; }

	protected LobbyNotificationMessage? notification;

	public override bool? CanVent()
	{
		if (!OptionGroupSingleton<VesselOptions>.Instance.VentingGhostsCanVent)
		{
			return null;
		}

		return VesselControlState.HasControl(Player.PlayerId) &&
			   (Player.Data.IsDead || Player.inVent);
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

/// <summary>
/// Defined for modifiers which keep track of a <see cref="Roles.Crewmate.VesselRole"/> player.
/// </summary>
public interface IVesselModifier
{
	/// <summary>
	/// The <see cref="Roles.Crewmate.VesselRole"/> player.
	/// </summary>
	public PlayerControl Vessel { get; }

	/// <summary>
	/// The Dead player, possessing.
	/// </summary>
	public PlayerControl Ghost { get; }

	public RoleBehaviour Role => Ghost.GetRoleWhenAlive();
}

/// <summary>
/// <inheritdoc/>
/// <para/>
/// Used specifically for the Ghost player.
/// </summary>
public interface IVesselSeekingModifier : IVesselModifier
{
}

/// <summary>
/// <inheritdoc/>
/// <para/>
/// Used specifically for a successful possession, given to both players (Ghost and Vessel) involved.
/// </summary>
public interface IVesselPossessModifier : IVesselModifier
{
	/// <summary>
	/// The other player that isn't the owner of the current modifier.
	/// </summary>
	public PlayerControl Target { get; }

	public void CreateNotification();

	public void ClearNotification();
}
