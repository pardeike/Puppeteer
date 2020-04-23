using System;
using UnityEngine;
using Verse;

namespace Puppeteer.Services
{
	[StaticConstructorOnStartup]
	public static class ConnectionStatus
	{
		public static void Update()
		{
			var n = OutgoingRequests.Count;
			var f = Math.Max(0f, Math.Min(1f, (float)n / OutgoingRequests.MaxQueued));

			var savedColor = GUI.color;

			var tex = Assets.status[Controller.instance.connection?.isConnected ?? false ? 1 : 0];
			var width = 70;
			var height = 25;
			var padding = 4;
			var r = new Rect(Screen.width - width - padding, padding, width, height);
			GUI.color = Color.white;
			tex.Draw(r, true);

			r.xMin += 1;
			r.yMin += 7;
			r.height = 11;
			r.width = f * 46;
			GUI.color = new Color(f, 1 - f, 0);
			GUI.DrawTexture(r.Rounded(), BaseContent.WhiteTex);

			GUI.color = savedColor;
		}
	}
}