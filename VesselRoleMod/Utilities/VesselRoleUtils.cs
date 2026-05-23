using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Usables;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VesselRoleMod.Utilities;

public static class VesselRoleUtils
{
	public static Vent? GetClosestUsableVent(this PlayerControl playerControl, bool forVenting)
	{
		Vector2 truePosition = playerControl.GetTruePosition();
		var flag2 = (playerControl.CanMove || playerControl.inVent || !forVenting);
		int num2 = Physics2D.OverlapCircleNonAlloc(truePosition, playerControl.MaxReportDistance,
			playerControl.hitBuffer, Constants.Usables);
		float num3 = float.MaxValue;
		List<Vent> list = new List<Vent>();
		for (int i = 0; i < num2; i++)
		{
			Collider2D collider2D = playerControl.hitBuffer[i];
			if (!playerControl.cache.TryGetValue(collider2D, out var array))
			{
				continue;
			}

			if (flag2)
			{
				foreach (var usable2 in array.Where(x => x.TryCast<Vent>() != null).Select(x => x.TryCast<Vent>()!))
				{
					float num4 = usable2.CanUse(forVenting, out bool flag4);
					if (flag4 && num4 < num3)
					{
						list.Add(usable2);
					}
				}
			}
		}
		var vent = (list.Count > 0) ? list.FirstOrDefault() : null;

		return vent;
	}

	public static float CanUse(this Vent vent, bool toVent, out bool couldUse)
	{
		PlayerControl player = PlayerControl.LocalPlayer;
		float num = float.MaxValue;
		couldUse = !toVent || !player.MustCleanVent(vent.Id) || Vent.currentVent == vent;

		var @event = new PlayerCanUseEvent(vent.Cast<IUsable>());
		MiraEventManager.InvokeEvent(@event);

		if (@event.IsCancelled)
		{
			couldUse = false;
			return num;
		}
		ISystemType systemType;
		if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out systemType))
		{
			var ventilationSystem = systemType.TryCast<VentilationSystem>();
			if (ventilationSystem != null && ventilationSystem.IsVentCurrentlyBeingCleaned(vent.Id))
			{
				couldUse = false;
			}
		}
		if (couldUse)
		{
			Vector3 center = player.Collider.bounds.center;
			Vector3 position = vent.transform.position;
			num = Vector2.Distance(center, position);
			couldUse &= (num <= vent.UsableDistance && !PhysicsHelpers.AnythingBetween(player.Collider, center, position, Constants.ShipOnlyMask, false));
		}
		return num;
	}
}
