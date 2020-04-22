using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class GeneralCommands
	{
		public static void SendAllColonists(Connection connection)
		{
			if (connection == null) return;
			var colonists = Tools.AllColonists(false)
				.Select(pawn =>
				{
					var puppet = State.Instance.PuppetForPawn(pawn);
					return new ColonistInfo()
					{
						id = pawn.thingIDNumber,
						name = pawn.OriginalName(),
						controller = puppet?.puppeteer?.vID,
					};
				})
				.ToList();
			connection.Send(new AllColonists() { colonists = colonists });
		}

		public static void Join(Connection connection, ViewerID vID)
		{
			if (vID.IsValid)
			{
				Tools.LogWarning($"{vID.name} joined");
				State.Instance.SetConnected(vID, true);
				var pawn = State.Instance.PuppeteerForViewer(vID)?.puppet?.pawn;
				if (pawn != null) Tools.SetColonistNickname(pawn, vID.name);
				State.Save();
				SendAllState(connection, vID);
			}
		}

		public static void Leave(ViewerID vID)
		{
			if (vID.IsValid)
			{
				Tools.LogWarning($"{vID.name} left");
				State.Instance.SetConnected(vID, false);
				State.Save();
			}
		}

		public static void Assign(Connection connection, Pawn pawn, ViewerID vID)
		{
			void SendAssignment(ViewerID v, bool state) => connection.Send(new Assignment() { viewer = v, state = state });

			if (vID == null)
			{
				if (pawn == null) return;
				Tools.SetColonistNickname(pawn, null);
				Tools.LogWarning($"{pawn.OriginalName()} lost control");
				vID = State.Instance.PuppetForPawn(pawn)?.puppeteer?.vID;
				State.Instance.Unassign(vID);
				if (vID != null) SendAssignment(vID, false);
				State.Save();
				return;
			}

			var oldPawn = State.Instance.PuppeteerForViewer(vID)?.puppet?.pawn;
			Tools.SetColonistNickname(oldPawn, null);
			State.Instance.Unassign(vID);

			var oldPuppeteer = State.Instance.PuppetForPawn(pawn)?.puppeteer;
			State.Instance.Unassign(oldPuppeteer?.vID);

			State.Instance.Assign(vID, pawn);
			Tools.SetColonistNickname(pawn, vID.name);

			Tools.LogWarning($"{pawn.OriginalName()} is now controlled by {vID.name}");
			SendAssignment(vID, true);
			State.Save();
		}

		/*public static IEnumerable<ViewerID> Available()
		{
			return State.instance.AvailableViewers();
		}*/

		static void SendGameInfo(Connection connection, ViewerID vID)
		{
			connection.Send(new GameInfo() { viewer = vID, info = new GameInfo.Info() { terrain = GridUpdater.ColorList() } });
		}

		public static void SendEarnToAll(Connection connection, int amount)
		{
			var puppeteers = State.Instance.ConnectedPuppeteers();
			puppeteers.Do(puppeteer =>
			{
				puppeteer.coinsEarned += amount;
				SendEarned(connection, puppeteer);
			});
		}

		static void SendEarned(Connection connection, State.Puppeteer puppeteer)
		{
			connection.Send(new Earned() { viewer = puppeteer.vID, info = new Earned.Info() { amount = puppeteer.coinsEarned } });
		}

		public static void SendPortrait(Connection connection, State.Puppeteer puppeteer)
		{
			var vID = puppeteer?.vID;
			var pawn = puppeteer?.puppet?.pawn;
			if (vID != null && pawn != null)
				OperationQueue.Add(OperationType.Portrait, () =>
				{
					var portrait = Renderer.GetPawnPortrait(pawn, new Vector2(35f, 55f));
					connection.Send(new Portrait() { viewer = vID, info = new Portrait.Info() { image = portrait } });
				});
		}

		static void SendStates<T>(Connection connection, string key, Func<Pawn, T> valueFunction, State.Puppeteer forPuppeteer = null)
		{
			void SendState(State.Puppeteer puppeteer)
			{
				var vID = puppeteer?.vID;
				var pawn = puppeteer?.puppet?.pawn;

				if (pawn != null)
					connection.Send(new OutgoingState<T>() { viewer = vID, key = key, val = valueFunction(pawn) });
			}

			if (forPuppeteer != null)
			{
				SendState(forPuppeteer);
				return;
			}
			var puppeteers = State.Instance.ConnectedPuppeteers();
			puppeteers.Do(p => SendState(p));
		}

		public static void SendAreas(Connection connection, State.Puppeteer forPuppeteer = null)
		{
			string[] GetResult(Pawn pawn)
			{
				return pawn.Map.areaManager.AllAreas
					.Where(a => a.AssignableAsAllowed())
					.Select(a => a.Label).ToArray();
			}
			SendStates(connection, "zones", GetResult, forPuppeteer);
		}

		public static void SendPriorities(Connection connection)
		{
			PrioritiyInfo GetResult(Pawn pawn)
			{
				int[] GetValues(Pawn p)
				{
					return Integrations.GetWorkTypeDefs().Select(def =>
					{
						var priority = p.workSettings.GetPriority(def);
						var passion = (int)p.skills.MaxPassionOfRelevantSkillsFor(def);
						var disabled = def.relevantSkills.Any(skill => p.skills.GetSkill(skill).TotallyDisabled);
						return disabled ? -1 : passion * 100 + priority;
					})
					.ToArray();
				}

				var columns = Integrations.GetWorkTypeDefs().Select(def => def.labelShort).ToArray();
				var rows = Tools.AllColonists(false)
					.Select(colonist => new PrioritiyInfo.Priorities() { pawn = colonist.LabelShortCap, yours = colonist == pawn, val = GetValues(colonist) })
					.ToArray();
				return new PrioritiyInfo()
				{
					columns = columns,
					manual = Current.Game.playSettings.useWorkPriorities,
					norm = Integrations.defaultPriority,
					max = Integrations.maxPriority,
					rows = rows
				};
			}
			SendStates(connection, "priorities", GetResult, null);
		}

		public static void SendSchedules(Connection connection)
		{
			ScheduleInfo GetResult(Pawn pawn)
			{
				string GetValues(Pawn p)
				{
					var schedules = Enumerable.Range(0, 24).Select(hour => p.timetable.GetAssignment(hour)).ToArray();
					return schedules.Join(s => Defs.Assignments[s], "");
				}
				var rows = Tools.AllColonists(false)
					.Select(colonist => new ScheduleInfo.Schedules() { pawn = colonist.LabelShortCap, yours = colonist == pawn, val = GetValues(colonist) })
					.ToArray();
				return new ScheduleInfo() { rows = rows };
			}
			SendStates(connection, "schedules", GetResult, null);
		}

		public static void SendAllState(Connection connection, ViewerID vID)
		{
			var puppeteer = State.Instance.PuppeteerForViewer(vID);

			SendGameInfo(connection, vID);
			SendEarned(connection, puppeteer);
			SendPortrait(connection, puppeteer);
			SendAreas(connection, puppeteer);
			SendPriorities(connection);
			SendSchedules(connection);
		}

		public static void UpdateGrids(Connection connection)
		{
			if (connection == null) return;

			var actions = State.Instance.ConnectedPuppeteers()
					.Select(puppeteer =>
					{
						var pawn = puppeteer.puppet?.pawn;
						if (pawn == null) return (Action)null;
						var gridSize = puppeteer.gridSize;
						if (gridSize == 0) return null;
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