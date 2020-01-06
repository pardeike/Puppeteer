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
		public Dictionary<string, Viewer> state = new Dictionary<string, Viewer>();

		public Viewers()
		{
			var data = Tools.ReadConfig(saveFileName);
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
			Tools.WriteConfig(saveFileName, sb.ToString());
		}

		public void Join(Connection connection, ViewerID vID)
		{
			if (vID.IsValid)
			{
				if (state.TryGetValue(vID.Identifier, out var viewer))
					viewer.connected = true;
				else
				{
					viewer = new Viewer() { vID = vID, name = vID.name, connected = true };
					state[vID.Identifier] = viewer;
				}
				SendEarned(connection, viewer);

				Log.Warning($"Viewer {vID} joined");
			}
		}

		public void Leave(ViewerID vID)
		{
			if (state.TryGetValue(vID.Identifier, out var viewer))
				viewer.connected = false;
			
			Log.Warning($"Viewer {vID} left");
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