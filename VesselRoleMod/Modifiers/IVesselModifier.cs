namespace VesselRoleMod.Modifiers;


/// <summary>
/// Defined for modifiers which keep track of a <see cref="Roles.Crewmate.VesselRole"/> player.
/// </summary>
public interface IVesselModifier
{
	/// <summary>
	/// The <see cref="Roles.Crewmate.VesselRole"/> player.
	/// </summary>
	public PlayerControl Vessel { get; }
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
	/// The Dead player, possessing.
	/// </summary>
	public PlayerControl Ghost { get; }

	/// <summary>
	/// The other player that isn't the owner of the current modifier.
	/// </summary>
	public PlayerControl Target { get; }

	public void CreateNotification();

	public void ClearNotification();
}
