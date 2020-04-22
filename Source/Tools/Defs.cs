using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class Defs
	{
		public static readonly Dictionary<TimeAssignmentDef, string> Assignments = new Dictionary<TimeAssignmentDef, string>()
		{
			{ TimeAssignmentDefOf.Anything, "A" },
			{ TimeAssignmentDefOf.Work, "W" },
			{ TimeAssignmentDefOf.Joy, "J" },
			{ TimeAssignmentDefOf.Sleep, "S" },
		};
	}
}