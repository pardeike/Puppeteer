using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace Puppeteer
{
	public static class StateCommand
	{
		public static void Set(Connection connection, IncomingState state)
		{
			if (connection == null) return;
			var vID = state.user;
			if (vID == null) return;
			var puppeteer = State.Instance.PuppeteerForViewer(vID);
			var pawn = puppeteer?.puppet?.pawn;
			if (pawn == null) return;

			if (puppeteer != null)
			{
				puppeteer.lastCommandIssued = DateTime.Now;
				puppeteer.lastCommand = $"set-{state.key}";
			}

			switch (state.key)
			{
				case "hostile-response":
					var responseMode = (HostilityResponseMode)Enum.Parse(typeof(HostilityResponseMode), state.val.ToString());
					pawn.playerSettings.hostilityResponse = responseMode;
					break;
				case "drafted":
					var drafted = Convert.ToBoolean(state.val);
					if (Tools.CannotMoveOrDo(pawn) == false)
						pawn.drafter.Drafted = drafted;
					break;
				case "zone":
					var area = pawn.Map.areaManager.AllAreas.Where(a => a.AssignableAsAllowed()).FirstOrDefault(a => a.Label == state.val.ToString());
					pawn.playerSettings.AreaRestriction = area;
					break;
				case "priority":
				{
					var val = Convert.ToInt32(state.val);
					var idx = val / 100;
					var prio = val % 100;
					var defs = Integrations.GetWorkTypeDefs().ToArray();
					if (idx >= 0 && idx < defs.Length)
						pawn.workSettings.SetPriority(defs[idx], prio);
					break;
				}
				case "schedule":
				{
					var pair = Convert.ToString(state.val).Split(':');
					if (pair.Length == 2)
					{
						var idx = Tools.SafeParse(pair[0]);
						if (idx.HasValue)
						{
							var type = Defs.Assignments.FirstOrDefault(ass => ass.Value == pair[1]).Key;
							pawn.timetable.SetAssignment(idx.Value, type);
						}
					}
					break;
				}
				case "grid":
				{
					var gridSize = Convert.ToInt32(state.val);
					puppeteer.gridSize = gridSize;
					if (gridSize > 0)
					{
						connection.Send(new GridUpdate()
						{
							controller = vID,
							info = new GridUpdate.Info()
							{
								px = pawn.Position.x,
								pz = pawn.Position.z,
								val = GridUpdater.GetGrid(pawn, gridSize)
							}
						});
					}
					break;
				}
				case "goto":
				{
					var val = Convert.ToString(state.val);
					var coordinates = val.Split(',').Select(v => { if (int.TryParse(v, out var n)) return n; else return -1000; }).ToArray();
					if (coordinates.Length == 2)
					{
						if (Tools.CannotMoveOrDo(pawn) == false)
						{
							var cell = new IntVec3(coordinates[0], 0, coordinates[1]);
							if (cell.InBounds(pawn.Map) && cell.Standable(pawn.Map))
							{
								var job = JobMaker.MakeJob(JobDefOf.Goto, cell);
								pawn.drafter.Drafted = true;
								pawn.jobs.StartJob(job, JobCondition.InterruptForced);
							}
						}
					}
					break;
				}
				default:
					Tools.LogWarning($"Unknown set value operation with key {state.key}");
					break;
			}
		}
	}
}