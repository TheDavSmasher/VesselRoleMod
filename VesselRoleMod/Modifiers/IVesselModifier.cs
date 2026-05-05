namespace VesselRoleMod.Modifiers;

public interface IVesselModifier
{
	public PlayerControl Vessel { get; }

	public PlayerControl Ghost { get; }

	public PlayerControl Target { get; }
}
