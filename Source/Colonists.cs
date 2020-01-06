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
		public Dictionary<string, Colonist> state = new Dictionary<string, Colonist>();

		public Colonists()
		{
			var data = Tools.ReadConfig(saveFileName);
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
			Tools.WriteConfig(saveFileName, sb.ToString());
		}

		public void SendAllColonists(Connection connection)
		{
			var pawns = Find.Maps.SelectMany(map => map.mapPawns.FreeColonists).ToList();
			PlayerPawnsDisplayOrderUtility.Sort(pawns);
			var colonists = pawns.Select(p =>
			{
				ViewerID controller = null;
				if (state.TryGetValue(p.ThingID, out var colonist))
					controller = colonist.controller;
				return new ColonistInfo()
				{
					id = p.ThingID,
					name = p.Name.ToStringShort,
					controller = controller,
					connected = colonist?.connected ?? false
				};
			}).ToList();
			connection.Send(new AllColonists() { colonists = colonists }.GetJSON());
		}

		public void Add(Pawn pawn)
		{
			if (state.ContainsKey(pawn.ThingID) == false)
				state[pawn.ThingID] = new Colonist();
		}

		public void Remove(Pawn pawn)
		{
			_ = state.Remove(pawn.ThingID);
		}

		public void Connect(Pawn pawn, ViewerID viewer)
		{
			if (state.TryGetValue(pawn.ThingID, out var colonist))
			{
				colonist.controller = viewer;
				colonist.connected = viewer != null;
			}
		}
	}
}