namespace VesselRoleMod;

public enum VesselModRpc : uint
{
	AdorcismStart = 100,
	AdorcismEnd,
	OffsetForVessel,
	VesselPossession,
	VesselEndPossession,
	VesselTriggerInteraction
}

internal enum VesselModInternalRpc : uint
{
	VesselInputUnreliable,
	PoltergeistInputUnreliable
}
