using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class RemoteActionExtension
	{
		public static void RemoteLog(this Pawn pawn, string action, Thing target = null)
		{
			Find.PlayLog.Add(new RemoteAction(pawn, action, target));

			var selected = Find.Selector.SelectedObjects;
			if (selected.Count != 1) return;
			if (selected[0] != pawn) return;
			var mainTabWindow_Inspect = (MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow;
			if (mainTabWindow_Inspect == null) return;
			var pawnLogTab = mainTabWindow_Inspect.CurTabs.OfType<ITab_Pawn_Log>().FirstOrDefault();
			if (pawnLogTab == null) return;
			pawnLogTab.cachedLogDisplay = null;
		}
	}

	public class RemoteAction : LogEntry
	{
		public Pawn pawn;
		public string text;
		public Thing target;

		public RemoteAction()
		{
		}

		public RemoteAction(Pawn pawn, string text, Thing target) : base()
		{
			this.pawn = pawn;
			this.text = text;
			this.target = target;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref pawn, "pawn");
			Scribe_Values.Look(ref text, "text");
			Scribe_References.Look(ref target, "target");
		}

		public override Texture2D IconFromPOV(Thing pov)
		{
			return Assets.puppeteerMote;
		}

		public override bool CanBeClickedFromPOV(Thing pov)
		{
			return (pov == target && CameraJumper.CanJump(pawn)) || (pov == pawn && CameraJumper.CanJump(target));
		}

		public override void ClickedFromPOV(Thing pov)
		{
			if (pov == pawn)
			{
				CameraJumper.TryJumpAndSelect(target);
				return;
			}
			if (pov == target)
			{
				CameraJumper.TryJumpAndSelect(pawn);
				return;
			}
		}

		public override bool Concerns(Thing t)
		{
			return t == pawn || t == target;
		}

		public override IEnumerable<Thing> GetConcerns()
		{
			if (pawn != null)
				yield return pawn;
			if (target != null)
				yield return target;
		}

		public override string ToGameStringFromPOV_Worker(Thing pov, bool forceLog)
		{
			return text;
		}
	}
}
