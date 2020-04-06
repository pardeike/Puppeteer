using HarmonyLib;
using Newtonsoft.Json;
using Puppeteer.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
				if (state.TryGetValue(vID.Identifier, out var viewer))
				{
					viewer.connected = true;
					var info = colonists.FindEntry(viewer.vID);
					viewer.controlling = info?.thingID == null ? null : Tools.ColonistForThingID(info.thingID);
				}
				else
				{
					viewer = new Viewer() { vID = vID, connected = true };
					state[vID.Identifier] = viewer;
				}
				Save();
				Tools.SetColonistNickname(viewer.controlling, vID.name);
				SendEarned(connection, viewer);
				SendPortrait(connection, viewer);
				SendState(connection, viewer);
			}
		}

		public void Leave(ViewerID vID)
		{
			if (state.TryGetValue(vID.Identifier, out var viewer))
			{
				viewer.connected = false;
				Tools.SetColonistNickname(viewer.controlling, null);
				viewer.controlling = null;
				Save();
			}
		}

		public Viewer FindViewer(ViewerID vID)
		{
			if (vID == null) return null;
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
			connection.Send(new Earned() { viewer = viewer.vID, info = new Earned.Info() { amount = viewer.coins } });
		}

		public static void SendPortrait(Connection connection, Viewer viewer)
		{
			if (viewer.controlling == null)
				return;

			OperationQueue.Add(OperationType.Portrait, () =>
			{
				var portrait = Renderer.GetPawnPortrait(viewer.controlling, new Vector2(35f, 55f));
				connection.Send(new Portrait() { viewer = viewer.vID, info = new Portrait.Info() { image = portrait } });
			});
		}

		static void SendState(Connection connection, Viewer viewer)
		{
			if (viewer.controlling?.Map != null)
			{
				var areas = viewer.controlling.Map.areaManager.AllAreas.Where(a => a.AssignableAsAllowed()).Select(a => a.Label).ToArray();
				connection.Send(new OutgoingState<string[]>() { viewer = viewer.vID, key = "zones", val = areas });
			}
		}
	}
}