using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using UnityEngine;
using VesselRoleMod.Roles.Crewmate;

namespace VesselRoleMod.Options.Roles.Crewmate;

public sealed class VesselOptions : AbstractOptionGroup<VesselRole>
{
	public override string GroupName => CustomRoleSingleton<VesselRole>.Instance.RoleName;
	public override Color GroupColor => VesselRoleModColors.Vessel;

	[ModdedNumberOption("VesselOptionAdorciseCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
	public float AdorciseCooldown { get; set; } = 25f;

	[ModdedNumberOption("VesselOptionAdorciseWindow", 5f, 30f, 2.5f, MiraNumberSuffixes.Seconds)]
	public float AdorciseWindow { get; set; } = 10f;

	[ModdedNumberOption("VesselOptionPossessionDuration", 5f, 30f, 1f, MiraNumberSuffixes.Seconds)]
	public float PossessionDuration { get; set; } = 15f;

	[ModdedNumberOption("VesselOptionMinPossessionLength", 2f, 15f, 0.5f, MiraNumberSuffixes.Seconds)]
	public float MinPossessionLength { get; set; } = 5f;


	[ModdedToggleOption("VesselOptionImpostorsCanPossess")]
	public bool CanHostImpostors { get; set; } = true;

	[ModdedToggleOption("VesselOptionNeutralsCanPossess")]
	public bool CanHostNeutrals { get; set; } = true;

	[ModdedToggleOption("VesselOptionAllowSharedControl")]
	public bool CanShareControl { get; set; } = true;

	[ModdedToggleOption("VesselOptionGhostCanVent")]
	public bool VentingGhostsCanVent { get; set; } = false;

	public ModdedToggleOption KillingGhostsCanKill { get; set; } = new("VesselOptionGhostCanKill", true);

	public ModdedEnumOption<VesselOnKillType> KillingGhostOnKill { get; set; } =
		new("VesselOptionOnGhostKill", VesselOnKillType.CannotKill,
			["VesselOptionOnGhostKillEnumNone", "VesselOptionOnGhostKillEnumCannotKill", "VesselOptionOnGhostKillEnumCannotPossess"])
		{
			Visible = () => OptionGroupSingleton<VesselOptions>.Instance.KillingGhostsCanKill.Value
		};

	// public ModdedToggleOption KillingInvestigative shenanigans

	public ModdedEnumOption<VesselRejectionType> CanRejectPossession { get; set; } =
		new("VesselOptionCanReject", VesselRejectionType.None,
			["VesselOptionCanRejectEnumNone", "VesselOptionCanRejectEnumBlacklistOnly", "VesselOptionCanRejectEnumFree"]);

	public ModdedToggleOption CanSeeGhostName { get; set; } = new("VesselOptionKnowsGhostName", false)
	{
		Visible = () => OptionGroupSingleton<VesselOptions>.Instance.CanRejectPossession != VesselRejectionType.Free
	};

	public ModdedNumberOption MaxDecisionTime { get; set; } =
		new("VesselOptionMaxDecisionTime", 5f, 3f, 10f, 0.5f, MiraNumberSuffixes.Seconds)
		{
			Visible = () => OptionGroupSingleton<VesselOptions>.Instance.CanRejectPossession == VesselRejectionType.Free
		};

	public bool NotifHasName => CanRejectPossession == VesselRejectionType.Free || CanSeeGhostName;
}

public enum VesselRejectionType
{
	None,
	BlacklistOnly,
	Free
}

public enum VesselOnKillType
{
	None,
	CannotKill,
	CannotPossess
}
