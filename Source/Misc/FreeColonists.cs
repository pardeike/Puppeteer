using System.Collections.Generic;
using Verse;

namespace Puppeteer
{
	public class FreeColonists
	{
		static readonly Dictionary<Map, List<Pawn>> pawns = new Dictionary<Map, List<Pawn>>();

		public static List<Pawn> Get(Map map)
		{
			if (pawns.TryGetValue(map, out var result))
				return result;
			return new List<Pawn>();
		}

		public static void Update(Map map)
		{
			pawns[map] = map.mapPawns.FreeColonists.ListFullCopy();
		}
	}
}