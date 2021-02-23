using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public class Settings
	{
		public int mapImageSize = 180;
		public int mapImageCompression = 9;
		public int mapUpdateFrequency = 600;
		public bool showOffLimitZones = true;
		public int startTickets = 20;
		public int playerActionCooldownTicks = GenDate.TicksPerHour;
		public bool sendChatResponsesToTwitch = false;

		public HashSet<string> menuCommands = new HashSet<string>();
	}

	public static class SettingsDrawer
	{
		public static string currentHelpItem = null;
		public static Vector2 scrollPosition = Vector2.zero;
		public static void DoWindowContents(ref Settings settings, Rect inRect)
		{
			inRect.yMin += 15f;
			inRect.yMax -= 15f;

			var firstColumnWidth = (inRect.width - Listing.ColumnSpacing) * 3.5f / 5f;
			var secondColumnWidth = inRect.width - Listing.ColumnSpacing - firstColumnWidth;

			var outerRect = new Rect(inRect.x, inRect.y, firstColumnWidth, inRect.height);
			var innerRect = new Rect(0f, 0f, firstColumnWidth - 24f, inRect.height);
			Widgets.BeginScrollView(outerRect, ref scrollPosition, innerRect, true);

			currentHelpItem = null;

			var list = new Listing_Standard();
			list.Begin(innerRect);

			{
				// About
				var intro = "Puppeteer";
				var textHeight = Text.CalcHeight(intro, list.ColumnWidth - 3f - Dialogs.inset) + 2 * 3f;
				Widgets.Label(list.GetRect(textHeight).Rounded(), intro);
				list.Gap(10f);

				list.Dialog_IntSlider("MapImageSize", n => $"{n}x{n} pixel", ref settings.mapImageSize, 32, 256);
				list.Dialog_IntSlider("MapImageCompression", n => $"{10 * n}%", ref settings.mapImageCompression, 1, 9);

				var oldVal = settings.mapUpdateFrequency;
				var val = settings.mapUpdateFrequency / 10;
				list.Dialog_IntSlider("MapUpdateFrequency", n => $"{n * 10} ms", ref val, 10, 200);
				settings.mapUpdateFrequency = val * 10;
				if (settings.mapUpdateFrequency != oldVal)
					GeneralCommands.SendGameInfoToAll();

				list.Gap(10f);
				list.Dialog_IntSlider("StartTickets", n => $"{n} tickets", ref settings.startTickets, 0, 100);
				list.Dialog_IntSlider("PlayerActionCooldownTicks", n => $"{Math.Floor((float)n / GenDate.TicksPerHour * 10 + 0.5) / 10} hour(s)", ref settings.playerActionCooldownTicks, 0, GenDate.TicksPerDay);

				list.Gap(10f);
				list.Dialog_Checkbox("SendChatResponsesToTwitch", ref settings.sendChatResponsesToTwitch);
			}

			list.End();
			Widgets.EndScrollView();

			if (currentHelpItem != null)
			{
				outerRect.x += firstColumnWidth + Listing.ColumnSpacing;
				outerRect.width = secondColumnWidth;

				list = new Listing_Standard();
				list.Begin(outerRect);

				var title = currentHelpItem.SafeTranslate().Replace(": {0}", "");
				list.Dialog_Label(title, false);

				list.Gap(8f);

				var text = (currentHelpItem + "_Help").SafeTranslate();
				var anchor = Text.Anchor;
				Text.Anchor = TextAnchor.MiddleLeft;
				var textHeight = Text.CalcHeight(text, list.ColumnWidth - 3f - Dialogs.inset) + 2 * 3f;
				var rect = list.GetRect(textHeight).Rounded();
				GUI.color = Color.white;
				Widgets.Label(rect, text);
				Text.Anchor = anchor;

				list.End();
			}
		}
	}
}
