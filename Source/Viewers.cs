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
			var save = Tools.ReadConfig(saveFileName);
			if (save != null)
			{
				var reader = new JsonReader(save);
				state = reader.Deserialize<Dictionary<string, Viewer>>();
			}
		}

		public void Save()
		{
			var sb = new StringBuilder();
			using (var writer = new JsonWriter(sb)) { writer.Write(state); }
			Tools.WriteConfig(saveFileName, sb.ToString());
		}

		public void Join(ViewerID vID)
		{
			if (state.TryGetValue($"{vID}", out var viewer))
			{
				viewer.connected = true;
			} else
				state[$"{vID}"] = new Viewer() { connected = true };
			
			Log.Warning($"Viewer {vID} joined");
		}

		public void Leave(ViewerID vID)
		{
			if (state.TryGetValue($"{vID}", out var viewer))
				viewer.connected = false;
			
			Log.Warning($"Viewer {vID} left");
		}

		public void Earn(Connection connection, int amount)
		{
			state.DoIf(viewer => viewer.Value.connected, viewer =>
			{
				viewer.Value.coins += amount;
				connection.Send(new Earned() { viewer = new ViewerID(viewer.Key), amount = viewer.Value.coins }.GetJSON());
			});
		}
	}
}