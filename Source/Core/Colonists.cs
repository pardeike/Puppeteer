using HarmonyLib;
using Newtonsoft.Json;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Puppeteer
{
	public class ColonistEntry
	{
		public int thingID;
		public Colonist colonist;

		public Pawn GetPawn()
		{
			return Tools.ColonistForThingID(thingID);
		}
	}

	public class Colonists
	{
		const string saveFileName = "PuppeteerColonists.json";

		// keys: ""+thingIDNumber
		public Dictionary<string, Colonist> state = new Dictionary<string, Colonist>();

		public Colonists()
		{
			var data = saveFileName.ReadConfig();
			if (data != null)
				state = JsonConvert.DeserializeObject<Dictionary<string, Colonist>>(data);
		}

		public void Save()
		{
			var data = JsonConvert.SerializeObject(state);
			saveFileName.WriteConfig(data);
		}

		public Pawn GetColonist(ViewerID viewer)
		{
			var entry = FindEntry(viewer);
			if (entry == null) return null;
			return entry.GetPawn();
		}

		public void SendAllColonists(Connection connection, bool forceUpdate)
		{
			if (connection == null) return;
			var colonists = Tools.AllColonists(forceUpdate).Select(p =>
			{
				ViewerID controller = null;
				if (state.TryGetValue("" + p.thingIDNumber, out var colonist))
					controller = colonist.controller;
				return new ColonistInfo()
				{
					id = p.thingIDNumber,
					name = p.Name.ToStringShort,
					controller = controller,
					lastSeen = colonist?.lastSeen ?? ""
				};
			}).ToList();
			connection.Send(new AllColonists() { colonists = colonists });
		}

		public ColonistEntry FindEntry(ViewerID viewer)
		{
			return state
				.Where(pair => pair.Value.controller == viewer)
				.Select(pair => new ColonistEntry() { thingID = int.Parse(pair.Key), colonist = pair.Value })
				.FirstOrDefault();
		}

		public Colonist FindColonist(Pawn pawn)
		{
			if (pawn == null) return null;
			if (state.TryGetValue("" + pawn.thingIDNumber, out var colonist))
				return colonist;
			return null;
		}

		public void Assign(string colonistID, ViewerID viewer, Connection connection)
		{
			void SendAssignment(ViewerID v, bool state) => connection.Send(new Assignment() { viewer = v, state = state });

			if (viewer == null)
			{
				if (state.TryGetValue(colonistID, out var current))
				{
					var controller = current?.controller;
					if (controller != null)
					{
						var entry = FindEntry(controller);
						Tools.SetColonistNickname(entry?.GetPawn(), null);
						SendAssignment(current.controller, false);
					}
				}
				_ = state.Remove(colonistID);
				Save();
				return;
			}
			state.DoIf(pair => pair.Value.controller == viewer, pair => SendAssignment(pair.Value.controller, false));
			_ = state.RemoveAll(pair => pair.Value.controller == viewer);

			if (state.TryGetValue(colonistID, out var colonist))
			{
				colonist.controller = viewer;
				Save();
				SendAssignment(viewer, true);
				return;
			}

			colonist = new Colonist() { controller = viewer };
			state[colonistID] = colonist;
			Save();
			SendAssignment(viewer, true);
		}

		public void SetState(IncomingState state)
		{
			var entry = FindEntry(state.user);
			if (entry == null) return;
			switch (state.key)
			{
				case "hostile-response":
					var responseMode = (HostilityResponseMode)Enum.Parse(typeof(HostilityResponseMode), state.val.ToString());
					OperationQueue.Add(OperationType.SetState, () =>
					{
						var pawn = entry.GetPawn();
						if (pawn != null)
							pawn.playerSettings.hostilityResponse = responseMode;
					});
					break;
				case "drafted":
					var drafted = Convert.ToBoolean(state.val);
					OperationQueue.Add(OperationType.SetState, () =>
					{
						var pawn = entry.GetPawn();
						if (pawn != null && pawn.Spawned)
							pawn.drafter.Drafted = drafted;
					});
					break;
				case "zone":
					OperationQueue.Add(OperationType.SetState, () =>
					{
						var pawn = entry.GetPawn();
						if (pawn != null)
						{
							var area = pawn.Map.areaManager.AllAreas.Where(a => a.AssignableAsAllowed()).FirstOrDefault(a => a.Label == state.val.ToString());
							pawn.playerSettings.AreaRestriction = area;
						}
					});
					break;
				case "priority":
				{
					var val = Convert.ToInt32(state.val);
					var idx = val / 100;
					var prio = val % 100;
					OperationQueue.Add(OperationType.SetState, () =>
					{
						var pawn = entry.GetPawn();
						if (pawn != null)
						{
							var defs = Integrations.GetWorkTypeDefs().ToArray();
							if (idx >= 0 && idx < defs.Length)
								pawn.workSettings.SetPriority(defs[idx], prio);
						}
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
								var pawn = entry.GetPawn();
								if (pawn != null)
									pawn.timetable.SetAssignment(idx.Value, type);
							});
						}
					}
					break;
				}
				default:
					Log.Warning($"Unknown set value operation with key {state.key}");
					break;
			}
		}
	}
}