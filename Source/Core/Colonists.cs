using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace Puppeteer
{
	public static class Colonists
	{
		public static void SendAllColonists(Connection connection)
		{
			if (connection == null) return;
			var colonists = State.instance.AllPuppets()
				.Select(puppet =>
				{
					var pawn = puppet.pawn;
					return new ColonistInfo()
					{
						id = pawn.thingIDNumber,
						name = pawn.Name.ToStringShort,
						controller = puppet.puppeteer?.vID,
					};
				})
				.ToList();
			connection.Send(new AllColonists() { colonists = colonists });
		}

		public static void Assign(Connection connection, int thingID, ViewerID vID)
		{
			void SendAssignment(ViewerID v, bool state) => connection.Send(new Assignment() { viewer = v, state = state });

			var pawn = Tools.ColonistForThingID(thingID);
			if (pawn == null) return;
			if (vID == null)
			{
				State.instance.Unassign(vID);
				State.instance.Save();
				Tools.SetColonistNickname(pawn, null);
				SendAssignment(vID, false);
				return;
			}
			State.instance.Assign(vID, pawn);
			State.instance.Save();
			Tools.SetColonistNickname(pawn, vID.name);
			SendAssignment(vID, true);
		}

		public static void SetState(Connection connection, IncomingState state)
		{
			if (connection == null) return;
			var vID = state.user;
			if (vID == null) return;
			var puppeteer = State.instance.PuppeteerForViewer(vID);
			var pawn = puppeteer?.puppet?.pawn;
			if (pawn == null) return;

			switch (state.key)
			{
				case "hostile-response":
					var responseMode = (HostilityResponseMode)Enum.Parse(typeof(HostilityResponseMode), state.val.ToString());
					OperationQueue.Add(OperationType.SetState, () =>
					{
						pawn.playerSettings.hostilityResponse = responseMode;
					});
					break;
				case "drafted":
					var drafted = Convert.ToBoolean(state.val);
					OperationQueue.Add(OperationType.SetState, () =>
					{
						if (Tools.CannotMoveOrDo(pawn) == false)
							pawn.drafter.Drafted = drafted;
					});
					break;
				case "zone":
					OperationQueue.Add(OperationType.SetState, () =>
					{
						var area = pawn.Map.areaManager.AllAreas.Where(a => a.AssignableAsAllowed()).FirstOrDefault(a => a.Label == state.val.ToString());
						pawn.playerSettings.AreaRestriction = area;
					});
					break;
				case "priority":
				{
					var val = Convert.ToInt32(state.val);
					var idx = val / 100;
					var prio = val % 100;
					OperationQueue.Add(OperationType.SetState, () =>
					{
						var defs = Integrations.GetWorkTypeDefs().ToArray();
						if (idx >= 0 && idx < defs.Length)
							pawn.workSettings.SetPriority(defs[idx], prio);
					});
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
							var type = Tools.Assignments.FirstOrDefault(ass => ass.Value == pair[1]).Key;
							OperationQueue.Add(OperationType.SetState, () =>
							{
								pawn.timetable.SetAssignment(idx.Value, type);
							});
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

		public static void UpdateGrids(Connection connection)
		{
			if (connection == null) return;

			var actions = State.instance.ConnectedPuppeteers()
					.Select(puppeteer =>
					{
						var pawn = puppeteer.puppet?.pawn;
						var gridSize = puppeteer.gridSize;
						if (gridSize == 0) return (Action)null;
						var vID = puppeteer.vID;
						return () =>
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
						};
					})
					.OfType<Action>()
					.ToList();
			Tools.RunEvery(15, actions);
		}
	}
}