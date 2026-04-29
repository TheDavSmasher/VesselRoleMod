namespace VesselRoleMod;

public enum VesselModRpc : uint
{
	AdorcismStart,
	AdorcismEnd,
	Possess,
	VesselPossession,
	VesselEndPossession,
	VesselTriggerInteraction
}

internal enum VesselModInternalRpc : uint
{
	VesselInputUnreliable
}
