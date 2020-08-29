using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Puppeteer
{
	public class PuppeteerSettingsTable : PawnTable
	{
		public PuppeteerSettingsTable(PawnTableDef def, Func<IEnumerable<Pawn>> _, int uiWidth, int uiHeight) : base(def, Pawns, uiWidth, uiHeight) { }

		public static IEnumerable<Pawn> Pawns()
		{
			var map = Find.CurrentMap;
			return State.Instance.AllPuppeteers()
				.Select(puppeteer => puppeteer.puppet?.pawn)
				.Where(pawn => map != null && pawn?.Map == map)
				.OfType<Pawn>();
		}
	}

	public class PuppeteerSettingsWindow : MainTabWindow_PawnTable
	{
		private static PawnTableDef pawnTableDef;
		protected override PawnTableDef PawnTableDef => pawnTableDef ?? (pawnTableDef = DefDatabase<PawnTableDef>.GetNamed("PuppeteerTableSettings"));
		protected override IEnumerable<Pawn> Pawns => Find.CurrentMap != null ? Find.CurrentMap.mapPawns.FreeColonists : new List<Pawn>();
		public override void PostOpen()
		{
			base.PostOpen();
			Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;
		}

		public override void PreClose()
		{
			base.PreClose();
			Tools.SetCurrentOffLimitsDesignator();
		}
	}
}