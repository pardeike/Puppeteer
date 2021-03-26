using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class Integrations
	{
		public static int defaultPriority = 3;
		public static int maxPriority = 4;

		static readonly Type t_PawnColumnWorker_WorkType;

		static T GetExternalFieldValue<T>(string path, T defaultValue)
		{
			var parts = path.Split(':');
			var t_Worktab_Settings = AccessTools.TypeByName(parts[0]);
			if (t_Worktab_Settings != null)
			{
				var f_maxPriority = t_Worktab_Settings.GetField(parts[1]);
				if (f_maxPriority != null)
					return (T)f_maxPriority.GetValue(null);
			}
			return defaultValue;
		}

		static Integrations()
		{
			defaultPriority = GetExternalFieldValue("WorkTab.Settings:defaultPriority", defaultPriority);
			maxPriority = GetExternalFieldValue("WorkTab.Settings:maxPriority", maxPriority);
			t_PawnColumnWorker_WorkType = AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType");
		}

		public static IEnumerable<WorkTypeDef> GetPawnWorkerDefs()
		{
			return PawnTableDefOf.Work.columns
				.Select(column => column.Worker)
				.Where(worker =>
				{
					if (worker is PawnColumnWorker_WorkPriority) return true;
					if (worker.GetType() == t_PawnColumnWorker_WorkType) return true;
					return false;
				})
				.Select(worker => worker.def?.workType)
				.Where(workType => workType != null);
		}
	}
}
