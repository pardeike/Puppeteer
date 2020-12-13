using HarmonyLib;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public class Dialog_EditOffLimitsArea : Window
	{
		const int maxRestrictions = 10;

		readonly OffLimitsArea area;
		bool firstTime = true;
		string areaName = "";

		public override Vector2 InitialSize => new Vector2(450f, 610f);

		public Dialog_EditOffLimitsArea(OffLimitsArea area)
		{
			this.area = area;
			areaName = area.label;

			forcePause = true;
			doCloseX = false;
			doCloseButton = true;
			closeOnClickedOutside = false;
			absorbInputAroundWindow = true;
		}

		public override void PostOpen()
		{
			base.PostOpen();
			Tools.SetCurrentOffLimitsDesignator(area, null);
		}

		public override void PreClose()
		{
			base.PreClose();
			if (areaName.Length > 0)
				area.label = areaName;

			Tools.SetCurrentOffLimitsDesignator();
		}

		bool NameIsValid(string name)
		{
			if (name.Length > 28) return false;
			var offLimits = Find.CurrentMap?.GetComponent<OffLimitsComponent>();
			if (offLimits == null) return false;
			return offLimits.areas.Any(a => a != area && a.label == name) == false;
		}

		static float ButtonWidth(string text)
		{
			return Text.CalcSize(text).x + 16f;
		}

		public override void DoWindowContents(Rect inRect)
		{
			var map = Find.CurrentMap;
			if (map == null) return;

			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
			{
				Event.current.Use();
				Close();
				return;
			}

			var list = new Listing_Standard { ColumnWidth = inRect.width };
			list.Begin(inRect);

			Text.Font = GameFont.Small;
			_ = list.Label("Name");

			GUI.SetNextControlName("RenameField");
			var newName = list.TextEntry(areaName);
			if (NameIsValid(newName))
				areaName = newName;

			list.Gap(10f);

			_ = list.Label("Color");
			DoColorSliders(list.GetRect(30f));

			list.Gap(10f);

			Text.Font = GameFont.Small;
			_ = list.Label("Restrictions (select the active ones)");

			var allRestrictions = map.GetComponent<OffLimitsComponent>().restrictions;
			var extra = (allRestrictions.Count < maxRestrictions ? 24f : -6f) + 1f;
#pragma warning disable CS0612
			var list2 = list.BeginSection(allRestrictions.Count * (24f + 6f) + extra);
#pragma warning restore CS0612
			var restrictions = allRestrictions.ToArray();
			for (var i = 0; i < restrictions.Length; i++)
			{
				DoRestrictionsRow(list2.GetRect(24f), restrictions[i]);
				list2.Gap(6f);
			}
			if (allRestrictions.Count < maxRestrictions)
			{
				var rect = list2.GetRect(24f);
				rect.width = ButtonWidth("Add new restriction") + 16f;
				if (Widgets.ButtonText(rect, "Add new restriction"))
				{
					for (var i = 1; true; i++)
					{
						var label = $"Restriction {i}";
						if (allRestrictions.All(r => r.label != label))
						{
							var newRestriction = new Restriction() { label = label };
							allRestrictions.Add(newRestriction);
							area.restrictions.Add(newRestriction);
							break;
						}
					}
				}
			}
			list.EndSection(list2);

			list.Dialog_Text(GameFont.Tiny, "EditAreaHelp");

			list.End();

			if (firstTime)
				UI.FocusControl("RenameField", this);
			firstTime = false;
		}

		public void DoRestrictionsRow(Rect rect, Restriction restriction)
		{
			GUI.BeginGroup(rect);
			var widgetRow = new WidgetRow(0f, 0f);

			var isActive = area.restrictions.Contains(restriction);
			Widgets.Checkbox(new Vector2(0f, 0f), ref isActive);
			if (isActive && area.restrictions.Contains(restriction) == false)
				area.restrictions.Add(restriction);
			if (isActive == false && area.restrictions.Contains(restriction))
				_ = area.restrictions.Remove(restriction);
			_ = widgetRow.Label("", 24f + 4f);

			_ = widgetRow.Label(restriction.label);
			widgetRow.Gap(rect.width - widgetRow.FinalX - ButtonWidth("Edit") - ButtonWidth("Delete") - WidgetRow.DefaultGap);

			if (widgetRow.ButtonText("Edit"))
				Find.WindowStack.Add(new Dialog_EditRestriction(restriction));

			if (widgetRow.ButtonText("Delete"))
			{
				var offLimits = Find.CurrentMap?.GetComponent<OffLimitsComponent>();
				if (offLimits != null)
				{
					var allRestrictions = offLimits.restrictions;
					_ = allRestrictions.Remove(restriction);
					offLimits.areas.Do(area => area.restrictions.Remove(restriction));
				}
			}

			GUI.EndGroup();
		}

		public void DoColorSliders(Rect rect)
		{
			rect.width = (rect.width - 2 * WidgetRow.DefaultGap) / 3f;
			var sum = area.color.r + area.color.g + area.color.b;

			area.color.r = Widgets.HorizontalSlider(rect, area.color.r, 0f, 1f, true, "Red");
			rect.x += rect.width + WidgetRow.DefaultGap;

			area.color.g = Widgets.HorizontalSlider(rect, area.color.g, 0f, 1f, true, "Green");
			rect.x += rect.width + WidgetRow.DefaultGap;

			area.color.b = Widgets.HorizontalSlider(rect, area.color.b, 0f, 1f, true, "Blue");

			if (area.color.r + area.color.g + area.color.b != sum)
				area.SetDirty();
		}
	}
}
