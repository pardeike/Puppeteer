using RimWorld;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Verse;

namespace Puppeteer
{
	public static class PlayerPawns
	{
		static readonly ConcurrentDictionary<Map, List<Pawn>> allPawns = new ConcurrentDictionary<Map, List<Pawn>>();
		static readonly ConcurrentDictionary<Map, List<Pawn>> freeColonists = new ConcurrentDictionary<Map, List<Pawn>>();

		public static List<Pawn> AllPawns(Map map)
		{
			if (allPawns.TryGetValue(map, out var result)) return result;
			return new List<Pawn>();
		}

		public static List<Pawn> FreeColonists(Map map, bool forceUpdatep)
		{
			if (forceUpdatep)
				Update(map);

			if (freeColonists.TryGetValue(map, out var result)) return result;
			return new List<Pawn>();
		}

		public static void Update(Map map)
		{
			var pawns = map.mapPawns;

			var all = pawns.PawnsInFaction(Faction.OfPlayer).ListFullCopy();
			_ = allPawns.AddOrUpdate(map, all, (m, o) => all);

			var free = pawns.FreeColonists.ListFullCopy();
			_ = freeColonists.AddOrUpdate(map, free, (m, o) => free);
		}
	}
}