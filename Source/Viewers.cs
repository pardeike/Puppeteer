using Harmony;
using JsonFx.Json;
using System.Collections.Generic;
using System.Text;
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
			{
				var reader = new JsonReader(data);
				state = reader.Deserialize<Dictionary<string, Viewer>>();
			}
		}

		public void Save()
		{
			var sb = new StringBuilder();
			using (var writer = new JsonWriter(sb)) { writer.Write(state); }
			saveFileName.WriteConfig(sb.ToString());
		}

		public void Join(Connection connection, Colonists colonists, ViewerID vID)
		{
			if (vID.IsValid)
			{
				if (state.TryGetValue(vID.Identifier, out var viewer))
				{
					viewer.connected = true;
					var thingID = colonists.FindThingID(viewer.vID);
					viewer.controlling = thingID == null ? null : Tools.ColonistForThingID(int.Parse(thingID));
				}
				else
				{
					viewer = new Viewer() { vID = vID, connected = true };
					state[vID.Identifier] = viewer;
				}
				SendEarned(connection, viewer);
			}
		}

		public void Leave(ViewerID vID)
		{
			if (state.TryGetValue(vID.Identifier, out var viewer))
			{
				viewer.connected = false;
				viewer.controlling = null;
			}
		}

		public Viewer FindViewer(ViewerID vID)
		{
			if (state.TryGetValue(vID.Identifier, out var viewer))
				return viewer;
			return null;
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
			connection.Send(new Earned() { viewer = viewer.vID, amount = viewer.coins }.GetJSON());
		}
	}
}