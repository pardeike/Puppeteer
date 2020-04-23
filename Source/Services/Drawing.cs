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
		static readonly long countdownTicks = t.AddSeconds(progressBarTimeInterval).Ticks - t.Ticks;
		static readonly Color barColor = new Color(0, 166 / 255f, 81 / 255f);

		public static void DrawAssignmentStatus(Pawn pawn, Rect rect)
		{
			if (Event.current.type != EventType.Repaint) return;

			if (pawn == null) return;
			var puppet = State.Instance.PuppetForPawn(pawn);
			var puppeteer = puppet?.puppeteer;
			if (puppeteer == null) return;

			var sixtySecondsAgo = DateTime.Now.Ticks - countdownTicks;
			var lastCommand = puppeteer.lastCommandIssued.Ticks;
			var deltaTicks = Math.Max(lastCommand, sixtySecondsAgo) - sixtySecondsAgo;
			var f = (float)((double)deltaTicks / countdownTicks);

			var savedColor = GUI.color;

			var tex = Assets.connected[puppeteer.connected ? 1 : 0];
			var height = rect.width * tex.height / tex.width;
			var r = new Rect((int)rect.xMin, (int)rect.yMin - (int)height, (int)rect.width, (int)height);
			GUI.color = new Color(1f, 1f, 1f, Find.ColonistBar.GetEntryRectAlpha(r));
			tex.Draw(r, true);

			if (puppeteer.connected && f > 0)
			{
				GUI.color = Color.white;
				var u = r.width / Find.ColonistBar.Size.x;
				r = new Rect(r.xMin, r.yMin + 4 * u, r.width, 6 * u);
				GUI.DrawTexture(r.Rounded(), BaseContent.BlackTex);
				r = r.ExpandedBy(-u);
				GUI.DrawTexture(r.Rounded(), BaseContent.WhiteTex);
				r.width *= f;
				GUI.color = barColor;
				GUI.DrawTexture(r.Rounded(), BaseContent.WhiteTex);
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