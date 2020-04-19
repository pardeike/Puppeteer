using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public class Viewers
	{
		const string saveFileName = "PuppeteerViewers.json";

		// keys: "{Service}:{ID}" (ViewerID.Identifier)
		public Dictionary<string, Viewer> state = new Dictionary<string, Viewer>();

		public Viewers()
		{
			var data = saveFileName.ReadConfig();
			if (data != null)
				state = JsonConvert.DeserializeObject<Dictionary<string, Viewer>>(data);
		}

		public void Save()
		{
			var data = JsonConvert.SerializeObject(state);
			saveFileName.WriteConfig(data);
		}

		public void Join(Connection connection, Colonists colonists, ViewerID vID)
		{
			if (vID.IsValid)
			{
				Tools.LogWarning($"{vID.name} joined");

				if (state.TryGetValue(vID.Identifier, out var viewer))
				{
					viewer.connected = true;
					var info = colonists.FindEntry(viewer.vID);
					if (info == null) return;
					info.colonist.gridSize = 0;
					viewer.controlling = info?.thingID == null ? null : Tools.ColonistForThingID(info.thingID);
				}
				else
				{
					viewer = new Viewer() { vID = vID, connected = true };
					state[vID.Identifier] = viewer;
				}
				Save();
				Tools.SetColonistNickname(viewer.controlling, vID.name);
				SendAllState(connection, viewer);
			}
		}

		public void Leave(ViewerID vID)
		{
			if (vID.IsValid)
			{
				if (state.TryGetValue(vID.Identifier, out var viewer))
				{
					Tools.LogWarning($"{vID.name} left");

					viewer.connected = false;
					Tools.SetColonistNickname(viewer.controlling, null);
					viewer.controlling = null;
					Save();
				}
			}
		}

		public List<Viewer> Available()
		{
			return state.Values
				.Where(viewer => viewer.controlling == null)
				.OrderBy(viewer => viewer.vID.name)
				.ToList();
		}

		public bool? ConnectedState(Pawn pawn)
		{
			if (pawn == null) return null;
			var result = state.Where(p => p.Value.controlling == pawn);
			if (result.Any() == false) return null;
			return result.First().Value.connected;
		}

		public Viewer FindViewer(ViewerID vID)
		{
			if (vID == null) return null;
			if (state.TryGetValue(vID.Identifier, out var viewer))
				return viewer;
			return null;
		}

		static void SendGameInfo(Connection connection, Viewer viewer)
		{
			connection.Send(new GameInfo() { viewer = viewer.vID, info = new GameInfo.Info() { terrain = GridUpdater.ColorList() } });
		}

		public void Earn(Connection connection, int amount)
		{
			state.DoIf(viewer => viewer.Value.connected, viewer =>
			{
				viewer.Value.coins += amount;
				SendEarned(connection, viewer.Value);
			});
		}

		static void SendEarned(Connection connection, Viewer viewer)
		{
			connection.Send(new Earned() { viewer = viewer.vID, info = new Earned.Info() { amount = viewer.coins } });
		}

		public static void SendPortrait(Connection connection, Viewer viewer)
		{
			OperationQueue.Add(OperationType.Portrait, () =>
			{
				var pawn = viewer.controlling;
				if (pawn != null)
				{
					var portrait = Renderer.GetPawnPortrait(pawn, new Vector2(35f, 55f));
					connection.Send(new Portrait() { viewer = viewer.vID, info = new Portrait.Info() { image = portrait } });
				}
			});
		}

		void SendStates<T>(Connection connection, string key, Func<Pawn, T> valueFunction, Viewer forViewer = null)
		{
			void SendState(Viewer viewer)
			{
				var pawn = viewer.controlling;
				if (pawn != null)
				{
					connection.Send(new OutgoingState<T>() { viewer = viewer.vID, key = key, val = valueFunction(viewer.controlling) });
				}
			}

			if (forViewer != null)
			{
				SendState(forViewer);
				return;
			}
			state.DoIf(viewer => viewer.Value.connected, pair => SendState(pair.Value));
		}

		public void SendAreas(Connection connection, Viewer forViewer = null)
		{
			string[] GetResult(Pawn pawn)
			{
				return pawn.Map.areaManager.AllAreas
					.Where(a => a.AssignableAsAllowed())
					.Select(a => a.Label).ToArray();
			}
			SendStates(connection, "zones", GetResult, forViewer);
		}

		public void SendPriorities(Connection connection)
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

		public void SendSchedules(Connection connection)
		{
			ScheduleInfo GetResult(Pawn pawn)
			{
				string GetValues(Pawn p)
				{
					var schedules = Enumerable.Range(0, 24).Select(hour => p.timetable.GetAssignment(hour)).ToArray();
					return schedules.Join(s => Tools.Assignments[s], "");
				}
				var rows = Tools.AllColonists(false)
					.Select(colonist => new ScheduleInfo.Schedules() { pawn = colonist.LabelShortCap, yours = colonist == pawn, val = GetValues(colonist) })
					.ToArray();
				return new ScheduleInfo() { rows = rows };
			}
			SendStates(connection, "schedules", GetResult, null);
		}

		public void SendAllState(Connection connection, Viewer viewer)
		{
			SendGameInfo(connection, viewer);
			SendEarned(connection, viewer);
			Tools.UpdateColonists(true);
			SendPortrait(connection, viewer);
			SendAreas(connection, viewer);
			SendPriorities(connection);
			SendSchedules(connection);
		}
	}
}