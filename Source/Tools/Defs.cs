using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class Defs
	{
		static Dictionary<TimeAssignmentDef, string> assignments;

		public static Dictionary<TimeAssignmentDef, string> Assignments
		{
			get
			{
				if (assignments == null)
				{
					assignments = new Dictionary<TimeAssignmentDef, string>()
					{
						{ TimeAssignmentDefOf.Anything, "A" },
						{ TimeAssignmentDefOf.Work, "W" },
						{ TimeAssignmentDefOf.Joy, "J" },
						{ TimeAssignmentDefOf.Sleep, "S" },
					};

					if (ModLister.RoyaltyInstalled)
					{
						var meditate = TimeAssignmentDefOf.Meditate;
						if (meditate != null)
							assignments.Add(meditate, "M");
					}
				}
				return assignments;
			}
		}
	}
}