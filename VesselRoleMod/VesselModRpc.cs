namespace VesselRoleMod;

public enum VesselModRpc : uint
{
	AdorcismStart,
	AdorcismEnd,
	OffsetForVessel,
	VesselPossession,
	VesselEndPossession,
	VesselTriggerInteraction
}

internal enum VesselModInternalRpc : uint
{
	VesselInputUnreliable
}
