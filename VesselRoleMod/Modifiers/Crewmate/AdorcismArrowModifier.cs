using MiraAPI.GameOptions;
using TownOfUs.Modules.RainbowMod;
using UnityEngine;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Crewmate;

/// <summary>
/// Modifier added to ghosts when a Vessel is performing an Adorcism, which gives an arrow to them to possess.
/// </summary>
/// <param name="owner">The Vessel player that the arrow points to</param>
public sealed class AdorcismArrowModifier(PlayerControl owner, Color color)
	: ArrowSourceModifier(owner, color, 0)
{
	public override float Duration => OptionGroupSingleton<VesselOptions>.Instance.AdorciseWindow;

	public override bool AutoStart => true;

	public override void OnActivate()
	{
		base.OnActivate();

		if (Arrow == null)
		{
			return;
		}

		var spr = Arrow.gameObject.GetComponent<SpriteRenderer>();
		var r = Arrow.gameObject.AddComponent<BasicRainbowBehaviour>();

		r.AddRend(spr, Player.cosmetics.ColorId);
	}
}
