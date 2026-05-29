namespace VesselRoleMod;

public enum VesselModRpc : uint
{
	AdorcismStart = 100,
	OffsetForVessel,
	VesselTryPossessing,
	VesselPossession,
	VesselEndPossession,
	VesselTriggerInteraction,
	VesselMoveVent,
	ChangePossessionControl
}

internal enum VesselModInternalRpc : uint
{
	VesselInputUnreliable,
	VesselStateUnreliable
}
