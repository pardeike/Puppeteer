using Harmony;
using JsonFx.Json;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Puppeteer
{
	public class Colonists
	{
		const string saveFileName = "PuppeteerColonists.json";

		// keys: ""+thingIDNumber
		public Dictionary<string, Colonist> state = new Dictionary<string, Colonist>();

		public Colonists()
		{
			var data = saveFileName.ReadConfig();
			if (data != null)
			{
				var reader = new JsonReader(data);
				state = reader.Deserialize<Dictionary<string, Colonist>>();
			}
		}

		public void Save()
		{
			var sb = new StringBuilder();
			using (var writer = new JsonWriter(sb)) { writer.Write(state); }
			saveFileName.WriteConfig(sb.ToString());
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
				if (state.TryGetValue(""+p.thingIDNumber, out var colonist))
					controller = colonist.controller;
				return new ColonistInfo()
				{
					id = p.thingIDNumber,
					name = p.Name.ToStringShort,
					controller = controller,
					lastSeen = colonist?.lastSeen ?? ""
				};
			}).ToList();
			connection.Send(new AllColonists() { colonists = colonists }.GetJSON());
		}

		public string FindThingID(ViewerID viewer)
		{
			return state
				.Where(pair => pair.Value.controller == viewer)
				.Select(pair => pair.Key)
				.FirstOrDefault();
		}

		public Colonist FindColonist(Pawn pawn)
		{
			if (state.TryGetValue("" + pawn.thingIDNumber, out var colonist))
				return colonist;
			return null;
		}

		public void Assign(int colonistID, ViewerID viewer)
		{
			if (viewer == null)
			{
				_ = state.Remove("" + colonistID);
				return;
			}
			_ = state.RemoveAll(pair => pair.Value.controller == viewer);

			if (state.TryGetValue("" + colonistID, out var colonist))
			{
				colonist.controller = viewer;
				return;
			}

			colonist = new Colonist() { controller = viewer };
			state["" + colonistID] = colonist;
		}
	}
}