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
		static readonly Color overrideColor = new Color(68 / 255f, 81 / 255f, 255f / 255f);

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

			var currentBarColor = barColor;
			var cooldown_f = puppet.CooldownFactor();
			var tex = Assets.connected[puppeteer.stalling ? 2 : (puppeteer.connected ? 1 : 0)];
			var height = rect.width * tex.height / tex.width;
			var baseRect = new Rect((int)rect.xMin, (int)rect.yMin - (int)height, (int)rect.width, (int)height).Rounded();
			if (cooldown_f <= 0)
			{
				GUI.color = new Color(1f, 1f, 1f, Find.ColonistBar.GetEntryRectAlpha(baseRect));
				tex.Draw(baseRect, true);
			}
			else
				f = cooldown_f;

			if ((puppeteer.IsConnected || cooldown_f > 0) && f > 0)
			{
				GUI.color = Color.white;
				var u = rect.width / tex.width;
				var r = baseRect;
				r.yMin += 16 * u;
				r.height = 24 * u;
				r = r.Rounded();
				GUI.DrawTexture(r, BaseContent.BlackTex);
				r = r.ExpandedBy(-(float)Math.Max(1, Math.Round(4f * u)));
				GUI.DrawTexture(r, BaseContent.WhiteTex);
				var oldWidth = r.width;
				r.width *= f;
				GUI.color = cooldown_f > 0 ? overrideColor : currentBarColor;
				GUI.DrawTexture(r, BaseContent.WhiteTex);
			}

			if (cooldown_f > 0)
			{
				GUI.color = new Color(1f, 1f, 1f, Find.ColonistBar.GetEntryRectAlpha(baseRect));
				Assets.overwritten.Draw(baseRect, true);
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

			if (existingPuppeteer == null && (e.modifiers & EventModifiers.Alt) != 0 && availablePuppeteers.Count > 0)
			{
				Controller.instance.AssignViewerToPawn(availablePuppeteers.RandomElement().vID, pawn);
				return;
			}

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
