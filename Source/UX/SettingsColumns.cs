using RimWorld;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public class CopyPastePuppeteerSettings : PawnColumnWorker_CopyPaste
	{
		class PuppetSetting
		{
			public static PuppetSetting From(Pawn pawn)
			{
				_ = pawn;
				return new PuppetSetting();
			}

			public void ApplyTo(Pawn pawn)
			{
				_ = pawn;
			}
		}

		private static PuppetSetting clipboard;
		protected override bool AnythingInClipboard => clipboard != null;

		public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
		{
			if (pawn.timetable == null) return;
			base.DoCell(rect, pawn, table);
		}

		protected override void CopyFrom(Pawn p)
		{
			clipboard = PuppetSetting.From(p);
		}

		protected override void PasteTo(Pawn p)
		{
			clipboard.ApplyTo(p);
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
			return pawn.Drafted;
		}

		protected override void SetValue(Pawn pawn, bool value)
		{
			pawn.drafter.Drafted = value;
		}
	}

	class PawnColumnWorker_PuppetOffLimits : PawnColumnWorker
	{
		const int TopAreaHeight = 65;
		const int ManageAreasButtonHeight = 32;
		protected override GameFont DefaultHeaderFont => GameFont.Tiny;

		public override int GetMinWidth(PawnTable table)
		{
			return Mathf.Max(base.GetMinWidth(table), 200);
		}

		public override int GetOptimalWidth(PawnTable table)
		{
			return Mathf.Clamp(273, GetMinWidth(table), GetMaxWidth(table));
		}

		public override int GetMinHeaderHeight(PawnTable table)
		{
			return Mathf.Max(base.GetMinHeaderHeight(table), TopAreaHeight);
		}

		public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
		{
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
			return base.GetHeaderTip(table) + "\n" + "Restrict controlled colonists by defining areas that do not allow controlling.";
		}
	}
}