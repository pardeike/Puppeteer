using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Puppeteer
{
	public class CopyPastePuppeteerSettings : PawnColumnWorker_CopyPaste
	{
		private static PawnSettings clipboard;
		protected override bool AnythingInClipboard => clipboard != null;

		public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
		{
			if (pawn.timetable == null) return;
			base.DoCell(rect, pawn, table);
		}

		protected override void CopyFrom(Pawn p)
		{
			clipboard = new PawnSettings();
			var settings = PawnSettings.SettingsFor(p);
			clipboard.enabled = settings.enabled;
			clipboard.activeAreas.AddRange(settings.activeAreas);
		}

		protected override void PasteTo(Pawn p)
		{
			if (clipboard != null)
			{
				var settings = PawnSettings.SettingsFor(p);
				settings.enabled = clipboard.enabled;
				settings.activeAreas.Clear();
				settings.activeAreas.AddRange(clipboard.activeAreas);
			}
			clipboard = null;
		}
	}

	class PawnColumnWorker_PuppetEnabled : PawnColumnWorker_Checkbox
	{
		protected override bool HasCheckbox(Pawn pawn)
		{
			return true;
		}

		protected override bool GetValue(Pawn pawn)
		{
			return PawnSettings.SettingsFor(pawn).enabled;
		}

		protected override void SetValue(Pawn pawn, bool value)
		{
			PawnSettings.SettingsFor(pawn).enabled = value;
		}
	}

	class PawnColumnWorker_PuppetOffLimits : PawnColumnWorker
	{
		const int IdealWidth = 748;
		const int TopAreaHeight = 65;
		const int ManageAreasButtonHeight = 32;
		protected override GameFont DefaultHeaderFont => GameFont.Tiny;

		public override int GetMinWidth(PawnTable table)
		{
			return Mathf.Max(base.GetMinWidth(table), IdealWidth);
		}

		public override int GetOptimalWidth(PawnTable table)
		{
			return Mathf.Clamp(IdealWidth, GetMinWidth(table), GetMaxWidth(table));
		}

		public override int GetMinHeaderHeight(PawnTable table)
		{
			return Mathf.Max(base.GetMinHeaderHeight(table), TopAreaHeight);
		}

		static bool DrawBox(Rect rect, PawnSettings settings, OffLimitsArea area)
		{
			MouseoverSounds.DoRegion(rect);
			var areaEnabled = settings.activeAreas.Contains(area);

			rect.yMin += 2;
			rect.yMax -= 1;
			rect.xMax -= 1;
			GUI.DrawTexture(rect, area.ColorTexture);
			if (!areaEnabled)
				GUI.DrawTexture(rect, Assets.dimmer);

			Text.Anchor = TextAnchor.MiddleLeft;
			var rect2 = rect;
			rect2.xMin += 4f;
			if (areaEnabled) rect2.yMin -= 2f;
			GUI.color = new Color(1, 1, 1, areaEnabled ? 1f : 0.4f);
			Widgets.Label(rect2, area.label);
			GUI.color = Color.white;

			if (areaEnabled)
			{
				rect2 = rect;
				rect2.yMin = rect2.yMax - 3;
				rect2.height = 3;
				Widgets.DrawBoxSolid(rect2, new Color(0.62745f, 0, 0));
			}

			var over = Mouse.IsOver(rect);
			if (over)
			{
				GUI.DrawTexture(rect, Assets.highlight);

				Tools.SetCurrentOffLimitsDesignator(area, null);

				if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					if (areaEnabled)
						_ = settings.activeAreas.Remove(area);
					else
						_ = settings.activeAreas.Add(area);
					SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera(null);
				}
			}
			Text.Anchor = TextAnchor.UpperLeft;
			return over;
		}

		public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
		{
			var allAreas = Find.CurrentMap?.GetComponent<OffLimitsComponent>()?.areas;
			if (allAreas == null) return;
			var areaSettings = PawnSettings.SettingsFor(pawn);
			Text.WordWrap = false;
			Text.Font = GameFont.Tiny;
			rect.width = Math.Min(140, rect.width / allAreas.Count);
			var over = false;
			for (var i = 0; i < allAreas.Count; i++)
			{
				over |= DrawBox(rect, areaSettings, allAreas[i]);
				rect.x += rect.width;
			}
			if (over == false && Find.WindowStack.IsOpen<Dialog_EditOffLimitsArea>() == false)
				Tools.SetCurrentOffLimitsDesignator();
			Text.WordWrap = true;
			Text.Font = GameFont.Small;
		}

		public override void DoHeader(Rect rect, PawnTable table)
		{
			if (Widgets.ButtonText(new Rect(rect.x, rect.y + (rect.height - 65f), Mathf.Min(rect.width, 360f), ManageAreasButtonHeight), "ManageAreas".Translate(), true, true, true))
			{
				Find.WindowStack.Add(new Dialog_OffLimits());
			}
		}

		public override int Compare(Pawn a, Pawn b)
		{
			return a.Label.CompareTo(b.Label);
		}

		protected override void HeaderClicked(Rect headerRect, PawnTable table)
		{
		}

		protected override string GetHeaderTip(PawnTable table)
		{
			return base.GetHeaderTip(table) + "\n" + "Restrict colonists by defining areas that do not allow certain viewer commands.";
		}
	}
}