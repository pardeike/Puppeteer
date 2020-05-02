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
					var grid = Tools.SafeParse(state.val, 4);
					if (grid == null)
					{
						connection.Send(new GridUpdate()
						{
							controller = vID,
							info = new GridUpdate.Info()
							{
								px = pawn.Position.x,
								pz = pawn.Position.z,
								width = 0,
								height = 0,
								val = Array.Empty<byte>()
							}
						});
					}
					else
					{
						puppeteer.grid = grid;
						connection.Send(new GridUpdate()
						{
							controller = vID,
							info = new GridUpdate.Info()
							{
								px = pawn.Position.x,
								pz = pawn.Position.z,
								width = grid[2] - grid[0] + 1,
								height = grid[3] - grid[1] + 1,
								val = GridUpdater.GetGrid(pawn, grid[0], grid[1], grid[2], grid[3])
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
				case "menu":
				{
					var val = Convert.ToString(state.val);
					var coordinates = val.Split(',').Select(v => { if (int.TryParse(v, out var n)) return n; else return -1000; }).ToArray();
					if (coordinates.Length == 2)
					{
						Actions.RemoveActions(pawn);
						if (Tools.CannotMoveOrDo(pawn) == false)
						{
							var cell = new IntVec3(coordinates[0], 0, coordinates[1]);
							if (cell.InBounds(pawn.Map))
							{
								var choices = FloatMenuMakerMap
									.ChoicesAtFor(cell.ToVector3(), pawn)
									.Select(choice =>
									{
										var id = Guid.NewGuid().ToString();
										Actions.AddAction(pawn, id, choice.action);
										return new ContextMenu.Choice()
										{
											id = id,
											label = choice.Label,
											disabled = choice.Disabled
										};
									}).ToArray();
								connection.Send(new ContextMenu()
								{
									controller = vID,
									choices = choices
								});
							}
						}
					}
					break;
				}
				case "action":
				{
					var id = Convert.ToString(state.val);
					_ = Actions.RunAction(pawn, id);
					break;
				}
				default:
					Tools.LogWarning($"Unknown set value operation with key {state.key}");
					break;
			}
		}
	}
}