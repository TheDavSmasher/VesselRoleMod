namespace VesselRoleMod;

public enum VesselModRpc : uint
{
	AdorcismStart = 100,
	AdorcismEnd,
	OffsetForVessel,
	VesselPossession,
	VesselEndPossession,
	VesselTriggerInteraction,
	ChangePossessionControl
}

internal enum VesselModInternalRpc : uint
{
	VesselInputUnreliable,
	PoltergeistInputUnreliable
}
