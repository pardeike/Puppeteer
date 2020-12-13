using RimWorld;
using System.Collections.Generic;

namespace Puppeteer
{
	public static class ApparelGroups
	{
		public delegate bool Matcher(ApparelProperties props);

		public struct Group
		{
			public string name;
			public Matcher matcher;
		}

		static readonly List<Group> groups = new List<Group>()
		{
			new Group()
			{
				name = "Head",
				matcher = new Matcher(a =>
				{
					if (a.layers.Contains(ApparelLayerDefOf.Overhead) == false) return false;
					return a.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead) || a.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead);
				})
			}
		};
	}
}
