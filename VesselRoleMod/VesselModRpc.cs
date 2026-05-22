namespace VesselRoleMod;

public enum VesselModRpc : uint
{
	AdorcismStart = 100,
	OffsetForVessel,
	VesselTryPossessing,
	VesselPossession,
	VesselEndPossession,
	VesselTriggerInteraction,
	ChangePossessionControl
}

internal enum VesselModInternalRpc : uint
{
	VesselInputUnreliable,
	VesselStateUnreliable
}
