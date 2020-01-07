using Harmony;
using JsonFx.Json;
using RimWorld;
using System;
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

		public void SendConnectAll(Connection connection)
		{
			state.DoIf(pair => pair.Value.controller != null, pair => SendConnected(connection, pair.Key, pair.Value.controller, true));
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
					lastSeen = colonist != null ? Tools.ConvertToUnixTimestamp(colonist.lastSeen) : 0
				};
			}).ToList();
			connection.Send(new AllColonists() { colonists = colonists }.GetJSON());
		}

		public static void SendConnected(Connection connection, string colonistID, ViewerID viewer, bool state)
		{
			Log.Warning($"CONNECTED {colonistID} {viewer} {state}");
			connection.Send(new Connected() { viewer = viewer.Simple, colonistID = colonistID, state = state }.GetJSON());
		}

		public void Assign(Connection connection, string colonistID, ViewerID viewer)
		{
			if (state.TryGetValue(colonistID, out var colonist))
			{
				var controller = colonist.controller;
				if (controller != null && viewer != controller)
					SendConnected(connection, colonistID, controller, false);
			}

			if (viewer == null)
			{
				_ = state.Remove(colonistID);
				return;
			}

			colonist = new Colonist() { controller = viewer };
			state[colonistID] = colonist;
			SendConnected(connection, colonistID, viewer, true);
		}

		public void KeepAlive(string colonistID, ViewerID viewer)
		{
			if (state.TryGetValue(colonistID, out var colonist))
			{
				if (colonist.controller == viewer)
					colonist.lastSeen = DateTime.Now;
			}
		}
	}
}