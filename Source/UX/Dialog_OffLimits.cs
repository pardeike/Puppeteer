using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public class Dialog_OffLimits : Window
	{
		public override Vector2 InitialSize => new Vector2(450f, 450f);
		bool showOffLimitZones;

		public Dialog_OffLimits()
		{
			forcePause = true;
			doCloseX = false;
			doCloseButton = true;
			closeOnClickedOutside = false;
			absorbInputAroundWindow = true;
		}

		public override void PostOpen()
		{
			base.PostOpen();
			showOffLimitZones = PuppeteerMod.Settings.showOffLimitZones;
			PuppeteerMod.Settings.showOffLimitZones = true;
		}

		public override void PreClose()
		{
			base.PreClose();
			PuppeteerMod.Settings.showOffLimitZones = showOffLimitZones;
		}

		public override void DoWindowContents(Rect inRect)
		{
			var map = Find.CurrentMap;
			if (map == null) return;

			var listing_Standard = new Listing_Standard { ColumnWidth = inRect.width };
			listing_Standard.Begin(inRect);
			var offLimits = map.GetComponent<OffLimitsComponent>();
			if (offLimits == null) return;
			var allAreas = offLimits.areas.ToArray().ToList();
			var selected = false;
			foreach (var area in allAreas)
			{
				selected |= DoAreaRow(listing_Standard.GetRect(24f), area);
				listing_Standard.Gap(6f);
			}
			if (selected == false && Find.WindowStack.IsOpen<Dialog_EditOffLimitsArea>() == false)
				Tools.SetCurrentOffLimitsDesignator();
			if (allAreas.Count < 10)
			{
				listing_Standard.Gap(30f * (9 - allAreas.Count));
				if (listing_Standard.ButtonText("NewArea".Translate(), null))
				{
					var area = new OffLimitsArea(map);
					offLimits.areas.Add(area);
				}
				listing_Standard.Dialog_Text(GameFont.Tiny, "CreateAreaHelp");
			}
			listing_Standard.End();
		}

		public bool DoAreaRow(Rect rect, OffLimitsArea area)
		{
			if (area == null) throw new ArgumentNullException(nameof(area));

			var labelWidth = rect.width;
			labelWidth -= 24f + 4f; // color
			labelWidth -= 24f + 4f; // +
			labelWidth -= 24f + 4f; // -
			labelWidth -= Text.CalcSize("Edit").x + 16f + 4f;
			labelWidth -= Text.CalcSize("InvertArea".Translate()).x + 16f + 4f;
			labelWidth -= 24f + 4f; // X

			var selected = false;
			var rect2 = rect;
			rect2.xMin += 24f + 4f;
			rect2.width = labelWidth;
			if (Mouse.IsOver(rect2))
			{
				Tools.SetCurrentOffLimitsDesignator(area, null);
				selected = true;

				GUI.color = area.color;
				Widgets.DrawHighlight(rect2);
				GUI.color = Color.white;

				var tip = area.restrictions?.Select(r => r?.label ?? "").Where(l => l.NullOrEmpty() == false).Join(null, "\n") ?? "";
				TooltipHandler.TipRegion(rect2, new TaggedString(tip));
			}

			GUI.BeginGroup(rect);
			var widgetRow = new WidgetRow(0f, 0f);
			_ = widgetRow.Icon(area.ColorTexture);

			_ = widgetRow.Label(area.label, labelWidth);

			if (widgetRow.ButtonIcon(Assets.AreaEdit[1]))
			{
				Close(true);
				Tools.SetCurrentOffLimitsDesignator(area, true);
			}

			if (widgetRow.ButtonIcon(Assets.AreaEdit[0]))
			{
				Close(true);
				Tools.SetCurrentOffLimitsDesignator(area, false);
			}

			if (widgetRow.ButtonText("Edit"))
				Find.WindowStack.Add(new Dialog_EditOffLimitsArea(area));

			if (widgetRow.ButtonText("InvertArea".Translate()))
				area.Invert();

			if (widgetRow.ButtonIcon(Assets.DeleteX, null, GenUI.SubtleMouseoverColor))
				area.Delete();
			GUI.EndGroup();

			return selected;
		}
	}
}