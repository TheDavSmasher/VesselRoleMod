namespace VesselRoleMod;

public enum VesselModRpc : uint
{
	AdorcismStart,
	AdorcismEnd,
	VesselPossession,
	VesselEndPossession,
	VesselTriggerInteraction
}

internal enum VesselModInternalRpc : uint
{
	VesselInputUnreliable
}
