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

		public void SendAllColonists(Connection connection)
		{
			if (connection == null) return;
			var allPawns = new List<Pawn>();
			Find.Maps.Do(map =>
			{
				var pawns = map.mapPawns.FreeColonists.ToList();
				PlayerPawnsDisplayOrderUtility.Sort(pawns);
				allPawns.AddRange(pawns);
			});
			Find.WorldObjects.Caravans
				.Where(caravan => caravan.IsPlayerControlled)
				.OrderBy(caravan => caravan.ID).Do(caravan =>
				{
					var pawns = caravan.PawnsListForReading;
					PlayerPawnsDisplayOrderUtility.Sort(pawns);
					allPawns.AddRange(pawns);
				});

			var colonists = allPawns.Select(p =>
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

			var pawn = FindEntry(viewer)?.GetPawn();
			var pawnNameTriple = pawn?.Name as NameTriple;

			if (viewer == null)
			{
				if (state.TryGetValue(colonistID, out var current))
					if (current.controller != null)
						SendAssignment(current.controller, false);
				_ = state.Remove(colonistID);
				Save();

				if (pawnNameTriple != null) pawn.Name = new NameTriple(pawnNameTriple.First, pawnNameTriple.First, pawnNameTriple.Last);

				return;
			}
			state.DoIf(pair => pair.Value.controller == viewer, pair => SendAssignment(pair.Value.controller, false));
			_ = state.RemoveAll(pair => pair.Value.controller == viewer);

			if (state.TryGetValue(colonistID, out var colonist))
			{
				colonist.controller = viewer;
				Save();
				if (pawnNameTriple != null) pawn.Name = new NameTriple(pawnNameTriple.First, viewer.name, pawnNameTriple.Last);
				SendAssignment(viewer, true);
				return;
			}

			colonist = new Colonist() { controller = viewer };
			state[colonistID] = colonist;
			Save();
			if (pawnNameTriple != null) pawn.Name = new NameTriple(pawnNameTriple.First, viewer.name, pawnNameTriple.Last);
			SendAssignment(viewer, true);
		}

		public void SetState(IncomingState state)
		{
			var entry = FindEntry(state.user);
			if (entry == null) return;
			var pawn = entry.GetPawn();
			if (pawn == null) return;
			switch (state.key)
			{
				case "hostile-response":
					var responseMode = (HostilityResponseMode)Enum.Parse(typeof(HostilityResponseMode), state.val.ToString());
					pawn.playerSettings.hostilityResponse = responseMode;
					break;
				case "drafted":
					pawn.drafter.Drafted = Convert.ToBoolean(state.val);
					break;
				case "zone":
					var area = pawn.Map.areaManager.AllAreas.Where(a => a.AssignableAsAllowed()).FirstOrDefault(a => a.Label == state.val.ToString());
					pawn.playerSettings.AreaRestriction = area;
					break;
				default:
					Log.Warning($"Unknown set value operation with key ${state.key}");
					break;
			}
		}
	}
}