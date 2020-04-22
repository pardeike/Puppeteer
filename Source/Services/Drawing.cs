using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class Drawing
	{
		const int progressBarTimeInterval = 30;

		static readonly DateTime t = DateTime.Now;
		static readonly long sixtySecondsTicks = t.AddSeconds(progressBarTimeInterval).Ticks - t.Ticks;
		static readonly Color barColor = new Color(0, 166 / 255f, 81 / 255f);

		public static void DrawAssignmentStatus(Pawn pawn, Rect rect)
		{
			if (pawn == null) return;
			var puppet = State.Instance.PuppetForPawn(pawn);
			var puppeteer = puppet?.puppeteer;
			if (puppeteer == null) return;

			var sixtySecondsAgo = DateTime.Now.Ticks - sixtySecondsTicks;
			var lastCommand = puppeteer.lastCommandIssued.Ticks;
			var deltaTicks = Math.Max(lastCommand, sixtySecondsAgo) - sixtySecondsAgo;
			var f = (float)((double)deltaTicks / sixtySecondsTicks);

			var savedColor = GUI.color;

			var standardWidth = rect.width == 48;
			var texture = standardWidth ? Assets.connectedMin : Assets.connectedMax;
			var tex = texture[puppeteer.connected ? 1 : 0];
			var height = rect.width * tex.height / tex.width;
			var r = new Rect((int)rect.xMin, (int)rect.yMin - (int)height, (int)rect.width, (int)height);
			GUI.color = new Color(1f, 1f, 1f, Find.ColonistBar.GetEntryRectAlpha(r));
			GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);

			if (puppeteer.connected)
			{
				var u = r.width / tex.width;
				var n = standardWidth ? 1 : 2;
				var r2 = new Rect(r.xMin + 1 * n, r.yMin + 5 * n, 46 * n * f, 4 * n);
				GUI.color = barColor;
				GUI.DrawTexture(r2, BaseContent.WhiteTex);
			}

			GUI.color = savedColor;
		}

		static Vector2 lastMousePos = Vector2.zero;
		public static void AssignFloatMenu(Pawn pawn, Rect rect)
		{
			var e = Event.current;
			if (e.type == EventType.MouseDown) lastMousePos = e.mousePosition;
			if (Mouse.IsOver(rect) == false) return;
			if (e.button != 1) return;
			if (e.type != EventType.MouseUp) return;
			if (Vector2.Distance(lastMousePos, e.mousePosition) > 4f) return;

			var puppet = State.Instance.PuppetForPawn(pawn);
			var puppeteer = puppet?.puppeteer;

			var availableViewers = State.Instance.AllPuppeteers().Select(p => p.vID).OrderBy(vID => vID.name).ToList();
			if (availableViewers.Any() || puppeteer != null)
			{
				var list = new List<FloatMenuOption>();
				if (puppeteer != null)
					list.Add(new FloatMenuOption($"Remove {puppeteer.vID.name}", () => Controller.instance.AssignViewerToPawn(null, pawn)));
				foreach (var vID in availableViewers)
					list.Add(new FloatMenuOption($"Assign {vID.name}", () => Controller.instance.AssignViewerToPawn(vID, pawn)));
				Find.WindowStack.Add(new FloatMenu(list));
			}
		}
	}
}