using System;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class Drawing
	{
		static readonly DateTime t = DateTime.Now;
		static readonly long sixtySecondsTicks = t.AddSeconds(60).Ticks - t.Ticks;
		static readonly Color barColor = new Color(0, 166 / 255f, 81 / 255f);

		public static void DrawAssignmentStatus(Pawn pawn, Rect rect)
		{
			if (pawn == null) return;
			var puppet = State.instance.PuppetForPawn(pawn);
			var puppeteer = puppet?.puppeteer;
			if (puppeteer == null) return;

			var sixtySecondsAgo = DateTime.Now.Ticks - sixtySecondsTicks;
			var lastCommand = puppeteer.lastCommandIssued.Ticks;
			var deltaTicks = Math.Max(lastCommand, sixtySecondsAgo) - sixtySecondsAgo;
			var f = (float)((double)deltaTicks / sixtySecondsTicks);

			var savedColor = GUI.color;

			var tex = Assets.connected[puppeteer.connected ? 1 : 0];
			var height = rect.width * tex.height / tex.width;
			var r = new Rect(rect.xMin, rect.yMin - height, rect.width, height);
			GUI.color = new Color(1f, 1f, 1f, Find.ColonistBar.GetEntryRectAlpha(r));
			GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);

			var u = r.width / tex.width;
			var r2 = new Rect(r.xMin + 2 * u, r.yMin + 10 * u, 92 * u * f, 8 * u);
			GUI.color = barColor;
			GUI.DrawTexture(r2, BaseContent.WhiteTex);

			GUI.color = savedColor;
		}
	}
}
