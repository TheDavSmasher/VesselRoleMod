using System;
using System.Collections.Generic;
using System.Linq;

namespace VesselRoleMod.Modules.ControlSystem;

public record PossessionKill(byte KillerId, byte VesselId, byte VictimId, DateTime KillTime);

public static class PossessionHistory
{
	public static readonly List<PossessionKill> GhostVesselKills = [];

	public static void AddMurder(PlayerControl killer, PlayerControl vessel, PlayerControl victim)
	{
		var deadBody = new PossessionKill(killer.PlayerId, vessel.PlayerId, victim.PlayerId, DateTime.UtcNow);

		GhostVesselKills.Add(deadBody);
	}

	public static void ClearMurder(PlayerControl player)
	{
		var instance = GhostVesselKills
			.Where(x => x.VictimId == player.PlayerId)
			.OrderByDescending(x => x.KillTime)
			.FirstOrDefault();

		if (instance == null)
		{
			return;
		}

		GhostVesselKills.Remove(instance);
	}

	public static void ClearAll()
	{
		GhostVesselKills.Clear();
	}
}
