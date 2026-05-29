namespace VesselRoleMod.Modifiers;

public interface IVesselPossessModifier
{
	public PlayerControl Vessel { get; }

	public PlayerControl Ghost { get; }

	public PlayerControl Target { get; }

	public void CreateNotification();

	public void ClearNotification();
}
