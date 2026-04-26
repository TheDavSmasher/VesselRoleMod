using MiraAPI.GameOptions;
using TownOfUs.Modifiers;
using TownOfUs.Modules.RainbowMod;
using UnityEngine;
using VesselRoleMod.Options.Roles.Crewmate;

namespace VesselRoleMod.Modifiers.Crewmate;

/// <summary>
/// Modifier added to the Vessel when performing an Adorcism, which gives an arrow to all ghosts that can possess.
/// </summary>
/// <param name="owner">The ghost player that can see the arrow</param>
public sealed class AdorcismArrowModifier(PlayerControl owner, Color color)
	: ArrowTargetModifier(owner, color, 0)
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
