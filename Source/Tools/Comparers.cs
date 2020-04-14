using System.Collections.Generic;
using Verse;

namespace Puppeteer
{
	public class MarketValueSorter : IComparer<Thing>
	{
		public int Compare(Thing x, Thing y)
		{
			return y.MarketValue.CompareTo(x.MarketValue);
		}
	}

	public class DistanceSorter : IComparer<Thing>
	{
		IntVec3 from;

		public DistanceSorter(IntVec3 from)
		{
			this.from = from;
		}

		public int Compare(Thing x, Thing y)
		{
			var dx = from.DistanceTo(x.Position);
			var dy = from.DistanceTo(y.Position);
			if (dx == dy) return 0;
			return dx < dy ? -1 : 1;
		}
	}
}