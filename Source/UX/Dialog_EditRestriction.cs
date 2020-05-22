using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WebSocketSharp;
using static HarmonyLib.AccessTools;

namespace Puppeteer
{
	public class Dialog_EditRestriction : Window
	{
		readonly Restriction restriction;
		bool firstTime = true;
		static Vector2 scroll1 = Vector2.zero;
		static Vector2 scroll2 = Vector2.zero;
		static Vector2 scroll3 = Vector2.zero;
		string restrictionName = "";

		static readonly Anchor[] anchors = Enum.GetValues(typeof(Anchor)).Cast<Anchor>().ToArray();

		public override Vector2 InitialSize => new Vector2(610f, 610f);

		public Dialog_EditRestriction(Restriction restriction)
		{
			this.restriction = restriction;
			restrictionName = restriction.label;

			forcePause = true;
			doCloseX = false;
			doCloseButton = true;
			closeOnClickedOutside = false;
			absorbInputAroundWindow = true;
		}

		public override void PreClose()
		{
			base.PreClose();
			if (NameIsValid(restrictionName))
				restriction.label = restrictionName;
		}

		bool NameIsValid(string name)
		{
			return name.Length <= 28;
		}

		static float ButtonWidth(string text)
		{
			return Text.CalcSize(text).x + 16f;
		}

		static void DrawBox(Rect rect)
		{
			var b = new Vector2(rect.x, rect.y);
			var a = new Vector2(rect.x + rect.width, rect.y + rect.height);
			var vector = a - b;
			GUI.DrawTexture(new Rect(b.x, b.y, 1, vector.y), Assets.tagBackground);
			GUI.DrawTexture(new Rect(a.x - 1, b.y, 1, vector.y), Assets.tagBackground);
			GUI.DrawTexture(new Rect(b.x + 1, b.y, vector.x - 2, 1), Assets.tagBackground);
			GUI.DrawTexture(new Rect(b.x + 1, a.y - 1, vector.x - 2, 1), Assets.tagBackground);
		}

		static void DrawTags(Rect rect, ref Vector2 scroll, string title, List<string> tags)
		{
			DrawBox(rect);
			var outerRect = rect.ExpandedBy(-5);
			Text.Font = GameFont.Tiny;
			outerRect.yMin -= 2f;
			Widgets.Label(outerRect, title);
			outerRect.yMin += 20f;
			var textHeight = Text.CalcSize("M").y - 2f;
			var innerRect = GenUI.DrawElementStack(new Rect(0, 0, outerRect.width - 16f, 99999f), textHeight, tags, (r, s) => { }, str => Text.CalcSize(str).x + 6f, 3f, 3f, false);
			Widgets.BeginScrollView(outerRect, ref scroll, innerRect, true);
			_ = GenUI.DrawElementStack(new Rect(0, 0, innerRect.width - 16f, 99999f), textHeight, tags, (r, s) =>
			{
				Widgets.DrawAtlas(r, Assets.tagBackground);
				var oldAnchor = Text.Anchor;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(r, s);
				Text.Anchor = oldAnchor;
			}, str => Text.CalcSize(str).x + 6f, 3f, 3f, false);
			Widgets.EndScrollView();
		}

		public override void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = inRect.width };
			list.Begin(inRect);

			Text.Font = GameFont.Small;
			_ = list.Label("Restriction Name");

			GUI.SetNextControlName("RenameField");
			var newName = list.TextEntry(restrictionName);
			if (NameIsValid(newName))
				restrictionName = newName;

			list.Gap(10f);

			_ = list.Label("Restricted terms");

			var allMatchers = restriction.matchers;
			var matchers = allMatchers.ToArray();

			var outerRect = list.GetRect(inRect.height / 2 - list.CurHeight - 12f);
			var innerRect = new Rect(0f, 0f, list.ColumnWidth - (matchers.Length > 6 ? 24f : 0f), matchers.Length * (24f + 6f));
			Widgets.BeginScrollView(outerRect, ref scroll1, innerRect, true);
			var list2 = new Listing_Standard() { ColumnWidth = innerRect.width };
			list2.Begin(innerRect);

			for (var i = 0; i < matchers.Length; i++)
			{
				DoMatcherRow(list2.GetRect(24f), i, matchers[i]);
				list2.Gap(6f);
			}

			list2.End();
			Widgets.EndScrollView();

			var rect = list.GetRect(24f);
			rect.width = ButtonWidth("Add") + 16f;
			if (Widgets.ButtonText(rect, "Add"))
				allMatchers.Add(new Matcher("", Anchor.Contains, false));

			list.Gap(20f);
			_ = list.Label("Match preview:");
			list.Gap(4f);

			var leftRect = new Rect(0, list.CurHeight, inRect.width, inRect.height - list.CurHeight - 68f);
			leftRect.width = leftRect.width / 2f - 4f;
			DrawTags(leftRect, ref scroll2, "Button actions (for selected items)", GetMatchingButtons());
			var rightRect = leftRect;
			rightRect.x += rightRect.width + 8f;
			DrawTags(rightRect, ref scroll3, "Floating menu items (seen so far)", GetMatchingMenus());

			list.End();

			if (firstTime)
				UI.FocusControl("RenameField", this);
			firstTime = false;
		}

		public void DoMatcherRow(Rect rect, int i, Matcher matcher)
		{
			var width1 = ButtonWidth("Contains");
			var width2 = ButtonWidth("Case matters");

			rect.width -= width1 + 4f;
			rect.width -= width2 + 4f;
			rect.width -= 24f + 4f;
			GUI.SetNextControlName($"matcher-{i}");
			var oldText = matcher.text;
			matcher.text = Widgets.TextField(rect, matcher.text);
			if (matcher.text != oldText)
				matcher.UpdateRegex();

			rect.x += rect.width + 4f;
			rect.width = width1;
			if (Widgets.ButtonText(rect, matcher.anchor.ToString()))
			{
				var next = ((int)matcher.anchor + 1) % Enum.GetValues(typeof(Anchor)).Length;
				matcher.anchor = anchors[next];
				matcher.UpdateRegex();
			}

			rect.x += rect.width + 4f;
			rect.width = width2;
			if (Widgets.ButtonText(rect, matcher.caseSensitive ? "Case matters" : "Case ignored"))
			{
				matcher.caseSensitive = matcher.caseSensitive == false;
				matcher.UpdateRegex();
			}

			rect.x += rect.width + 4f;
			rect.width = 24f;
			if (Widgets.ButtonImage(rect, Assets.DeleteX))
				_ = restriction.matchers.Remove(matcher);
		}

		static List<string> GetLabels(ISelectable thing)
		{
			var saved = new List<object>(Find.Selector.SelectedObjects);
			try
			{
				Find.Selector.SelectedObjects.Clear();
				Find.Selector.SelectedObjects.Add(thing);
				return thing.GetGizmos()
					.OfType<Command>()
					.Select(cmd => cmd?.LabelCap)
					.ToList();
			}
			catch (Exception)
			{
				return new List<string>();
			}
			finally
			{
				Find.Selector.SelectedObjects.Clear();
				Find.Selector.SelectedObjects.AddRange(saved);
			}
		}

		static readonly FieldRef<ThingGrid, List<Thing>[]> thingGrid = FieldRefAccess<ThingGrid, List<Thing>[]>("thingGrid");
		public List<string> GetMatchingButtons()
		{
			var fc = GUI.GetNameOfFocusedControl();
			if (fc.NullOrEmpty() || fc.StartsWith("matcher-") == false) return new List<string>();
			if (int.TryParse(fc.Substring(8), out var i) == false) return new List<string>();
			var matcher = restriction.matchers[i];
			if (matcher.text.IsNullOrEmpty()) return new List<string>();

			var labels = new HashSet<string>();
			labels.AddRange(
				Find.ReverseDesignatorDatabase.AllDesignators.Select(des => des.LabelCap)
			);
			labels.AddRange(
				thingGrid(Find.CurrentMap.thingGrid)
					.SelectMany(g => g)
					.Where(thing => ThingSelectionUtility.SelectableByMapClick(thing))
					.SelectMany(thing => GetLabels(thing))
			);
			labels.AddRange(
				Find.CurrentMap.zoneManager.AllZones
					.SelectMany(zone => GetLabels(zone))
			);

			return labels
				.Where(label => label != null && matcher.IsMatch(label))
				.OrderBy(s => s)
				.ToList();
		}

		public List<string> GetMatchingMenus()
		{
			var fc = GUI.GetNameOfFocusedControl();
			if (fc.NullOrEmpty() || fc.StartsWith("matcher-") == false) return new List<string>();
			if (int.TryParse(fc.Substring(8), out var i) == false) return new List<string>();
			var matcher = restriction.matchers[i];
			if (matcher.text.IsNullOrEmpty()) return new List<string>();

			return Puppeteer.Settings.menuCommands
				.Where(label => label != null && matcher.IsMatch(label))
				.OrderBy(s => s)
				.ToList();
		}
	}
}