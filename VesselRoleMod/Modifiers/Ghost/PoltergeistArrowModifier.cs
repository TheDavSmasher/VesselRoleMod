using MiraAPI.GameOptions;
using TownOfUs.Modules.RainbowMod;
using UnityEngine;
using VesselRoleMod.Modules.ControlSystem;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Ghost;

/// <summary>
/// Modifier added to ghosts when a Vessel is performing an Adorcism, which gives an arrow to them to possess.
/// </summary>
/// <param name="owner">The Vessel player that the arrow points to</param>
public sealed class PoltergeistArrowModifier(PlayerControl owner, Color color)
	: ArrowSourceModifier(owner, color, 0), IVesselModifier
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;

	public override bool AutoStart => true;

	public PlayerControl Vessel => Owner;
	public PlayerControl Ghost => Player;

	public override void FixedUpdate()
	{
		TimerActive = true;
		if (VesselControlState.IsPausingTimer(Owner.PlayerId))
		{
			TimerActive = false;
		}
		base.FixedUpdate();
	}

	public override void OnActivate()
	{
		base.OnActivate();

		if (Arrow == null)
		{
			return;
		}

		var spr = Arrow.gameObject.GetComponent<SpriteRenderer>();
		var r = Arrow.gameObject.AddComponent<BasicRainbowBehaviour>();

		r.AddRend(spr, Owner.cosmetics.ColorId);
	}
}
