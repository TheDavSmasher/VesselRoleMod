using MiraAPI.Modifiers;
using MiraAPI.PluginLoading;
using TownOfUs.Buttons;
using TownOfUs.Modules;
using UnityEngine;
using VesselRoleMod.Modifiers.Ghost;

namespace VesselRoleMod.Buttons.Modifiers;

[MiraIgnore]
public abstract class PoltergeistTargetButton<T> : TownOfUsTargetButton<T> where T : MonoBehaviour
{
	public override bool UsableInDeath => true;
	public override float InitialCooldown => 0.001f;
	public override Color TextOutlineColor => VesselRoleModColors.Vessel;

	protected static RoleBehaviour Role => PlayerControl.LocalPlayer.GetRoleWhenAlive();
	protected static PoltergeistModifier? Modifier => PlayerControl.LocalPlayer.GetModifier<PoltergeistModifier>();
	protected static PlayerControl? Vessel => Modifier?.Vessel;

	public override bool Enabled(RoleBehaviour? role)
	{
		return PlayerControl.LocalPlayer != null &&
			   PlayerControl.LocalPlayer.Data.IsDead &&
			   Modifier != null &&
			   CanUseAbility();
	}

	protected abstract bool CanUseAbility();
}
