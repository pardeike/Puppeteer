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

			if (pawn == null || pawn.Spawned == false) return;
			var puppet = State.Instance.PuppetForPawn(pawn);
			var puppeteer = puppet?.puppeteer;
			if (puppeteer == null) return;

			var sixtySecondsAgo = DateTime.Now.Ticks - countdownTicks;
			var lastCommand = puppeteer.lastCommandIssued.Ticks;
			var deltaTicks = Math.Max(lastCommand, sixtySecondsAgo) - sixtySecondsAgo;
			var f = (float)((double)deltaTicks / countdownTicks);

			var savedColor = GUI.color;

			var tex = Assets.connected[puppeteer.stalling ? 2 : (puppeteer.connected ? 1 : 0)];
			var height = rect.width * tex.height / tex.width;
			var r = new Rect((int)rect.xMin, (int)rect.yMin - (int)height, (int)rect.width, (int)height);
			GUI.color = new Color(1f, 1f, 1f, Find.ColonistBar.GetEntryRectAlpha(r));
			tex.Draw(r.Rounded(), true);

			if (puppeteer.IsConnected && f > 0)
			{
				GUI.color = Color.white;
				var u = rect.width / tex.width;
				r.yMin += 16 * u;
				r.height = 24 * u;
				r = r.Rounded();
				GUI.DrawTexture(r, BaseContent.BlackTex);
				r = r.ExpandedBy(-(float)Math.Max(1, Math.Round(4f * u)));
				GUI.DrawTexture(r, BaseContent.WhiteTex);
				r.width *= f;
				GUI.color = barColor;
				GUI.DrawTexture(r, BaseContent.WhiteTex);
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
			var existingPuppeteer = puppet?.puppeteer;

			var availablePuppeteers = State.Instance.ConnectedPuppeteers().OrderBy(p => p.vID.name).ToList();
			if (availablePuppeteers.Any() || existingPuppeteer != null)
			{
				var list = new List<FloatMenuOption>();
				if (existingPuppeteer != null)
					list.Add(new FloatMenuOption($"Remove {existingPuppeteer.vID.name}", () => Controller.instance.AssignViewerToPawn(null, pawn)));
				foreach (var puppeteer in availablePuppeteers)
					list.Add(new FloatMenuOption($"Assign {puppeteer.vID.name}", () => Controller.instance.AssignViewerToPawn(puppeteer.vID, pawn), puppeteer.puppet != null ? null : Assets.new27, Color.white));
				Find.WindowStack.Add(new FloatMenu(list));
			}
		}
	}
}