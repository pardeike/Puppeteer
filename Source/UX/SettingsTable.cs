using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Puppeteer
{
	public class PuppeteerSettingsTable : PawnTable
	{
		public PuppeteerSettingsTable(PawnTableDef def, Func<IEnumerable<Pawn>> pawnsGetter, int uiWidth, int uiHeight) : base(def, pawnsGetter, uiWidth, uiHeight) { }
	}

	public class PuppeteerSettingsWindow : MainTabWindow_PawnTable
	{
		private static PawnTableDef pawnTableDef;
		protected override PawnTableDef PawnTableDef => pawnTableDef ?? (pawnTableDef = DefDatabase<PawnTableDef>.GetNamed("PuppeteerTableSettings"));
		protected override IEnumerable<Pawn> Pawns => Find.CurrentMap.mapPawns.FreeColonists;
		public override void PostOpen()
		{
			base.PostOpen();
			Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;
		}
	}
}