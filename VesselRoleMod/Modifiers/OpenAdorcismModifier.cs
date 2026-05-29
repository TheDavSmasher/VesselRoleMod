using MiraAPI.GameOptions;
using MiraAPI.PluginLoading;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers;

[MiraIgnore]
public abstract class OpenAdorcismModifier : PossessionModifier
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;
	public override bool HideOnUi => true;
	public override PlayerControl Vessel => Player;

	public override void FixedUpdate()
	{
		TimerActive = true;
		if (VesselControlState.IsPausingTimer(Vessel.PlayerId))
		{
			TimerActive = false;
		}

		base.FixedUpdate();
	}
}
