namespace VesselRoleMod;

public enum VesselModRpc : uint
{
	AdorcismStart = 100,
	AdorcismEnd,
	OffsetForVessel,
	VesselTryPossessing,
	VesselPossession,
	VesselEndPossession,
	VesselTriggerInteraction,
	VesselSetGhostState,
	VesselMoveVent,
	ChangePossessionControl
}

internal enum VesselModInternalRpc : uint
{
	VesselInputUnreliable,
	VesselStateUnreliable
}
