using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Puppeteer
{
	public static class PlayerPawns
	{
		static readonly Dictionary<Map, List<Pawn>> allPawns = new Dictionary<Map, List<Pawn>>();
		static readonly Dictionary<Map, List<Pawn>> freeColonists = new Dictionary<Map, List<Pawn>>();

		public static List<Pawn> AllPawns(Map map)
		{
			if (allPawns.TryGetValue(map, out var result))
				return result;
			return new List<Pawn>();
		}

		public static List<Pawn> FreeColonists(Map map)
		{
			if (freeColonists.TryGetValue(map, out var result))
				return result;
			return new List<Pawn>();
		}

		public static void Update(Map map)
		{
			var pawns = map.mapPawns;
			allPawns[map] = pawns.PawnsInFaction(Faction.OfPlayer);
			freeColonists[map] = pawns.FreeColonists.ListFullCopy();
		}
	}
}