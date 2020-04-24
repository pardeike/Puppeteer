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
			var r = new Rect(UI.screenWidth - width - padding, padding, width, height);
			GUI.color = Color.white;
			tex.Draw(r, true);

			r.xMin += 2;
			r.yMin += 8;
			r.height = 9;
			r.width = f * 46;
			GUI.color = new Color(f, 1 - f, 0);
			GUI.DrawTexture(r.Rounded(), BaseContent.WhiteTex);

			var average = OutgoingRequests.AverageSendTime;
			if (average > 0)
			{
				r.xMin += 2;
				GUI.color = Color.white;
				foreach (var c in $"{average}$")
				{
					var numTex = c == '$' ? Assets.numbers[10] : Assets.numbers[c - '0'];
					r.width = numTex.width / 2f;
					GUI.DrawTexture(r, numTex);
					r.xMin += r.width + 1;
				}
			}

			GUI.color = savedColor;
		}
	}
}