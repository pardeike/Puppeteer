using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class GeneralGUI
	{
		public static void Update()
		{
			var n = OutgoingRequests.Count;
			var f = Math.Max(0f, Math.Min(1f, (float)n / OutgoingRequests.MaxQueued));

			var savedColor = GUI.color;

			var tex1 = Assets.status[Controller.instance.connection?.isConnected ?? false ? 1 : 0];
			var width = 70f;
			var height = 25f;
			var padding = 4;
			var r = new Rect(UI.screenWidth - width - padding, padding, width, height);
			GUI.color = Color.white;
			tex1.Draw(r, true);

			r.xMin += 2;
			r.yMin += 8;
			r.height = 9;
			r.width = f * 46;
			GUI.color = new Color(f, 1 - f, 0);
			GUI.DrawTexture(r.Rounded(), BaseContent.WhiteTex);

			var average = OutgoingRequests.AverageSendTime;
			if (average > 0)
			{
				r.xMin += 2;
				GUI.color = Color.white;
				foreach (var c in $"{average}$")
				{
					var numTex = c == '$' ? Assets.numbers[10] : Assets.numbers[c - '0'];
					r.width = numTex.width / 2f;
					GUI.DrawTexture(r, numTex);
					r.xMin += r.width + 1;
				}
			}

			var tex2 = Assets.colonist;
			width = tex2.width / 2;
			height = tex2.height / 2;
			r = new Rect(r.x - 2 * padding - width, r.center.y - height / 2, width, height);
			if (Widgets.ButtonImage(r, Assets.colonist))
				UnassignedViewersMenu();

			GUI.color = savedColor;
		}

		static void UnassignedViewersMenu()
		{
			var availableViewers = State.Instance.AllPuppeteers().Select(p => p.vID).OrderBy(vID => vID.name).ToList();
			if (availableViewers.Any())
			{
				var list = new List<FloatMenuOption>();
				foreach (var vID in availableViewers)
					list.Add(new FloatMenuOption(vID.name, () => GenerateColonist(vID)));
				Find.WindowStack.Add(new FloatMenu(list));
			}
		}

		static void GenerateColonist(ViewerID vID)
		{
			Current.Game.InitData = new GameInitData() { startingPawnCount = 1, playerFaction = Faction.OfPlayer };
			var newColonist = StartingPawnUtility.NewGeneratedStartingPawn();
			if (newColonist.Name is NameTriple triple)
				newColonist.Name = new NameTriple(triple.First, vID.name, triple.Last);
			Find.GameInitData.startingAndOptionalPawns.Add(newColonist);
			Find.WindowStack.Add(new Page_ConfigureStartingPawns()
			{
				next = null,
				nextAct = () =>
				{
					newColonist = Find.GameInitData.startingAndOptionalPawns[0];
					var map = Find.CurrentMap;
					if (CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out var cell))
					{
						_ = GenSpawn.Spawn(newColonist, cell, map, WipeMode.Vanish);
						ShowWandererJoinedLetter(newColonist);
						Controller.instance.AssignViewerToPawn(vID, newColonist);
					}
				}
			});
		}

		static void ShowWandererJoinedLetter(Pawn pawn)
		{
			var def = IncidentDefOf.WandererJoin;
			var baseLetterText = def.letterText.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true);
			var baseLetterLabel = def.letterLabel.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true);
			_ = PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref baseLetterText, ref baseLetterLabel, pawn);
			var worker = new IncidentWorker();
			var mSendStandardLetter = AccessTools.Method(typeof(IncidentWorker), "SendStandardLetter", new[] { typeof(TaggedString), typeof(TaggedString), typeof(LetterDef), typeof(IncidentParms), typeof(LookTargets), typeof(NamedArgument[]) });
			var parms = StorytellerUtility.DefaultParmsNow(def.category, Find.CurrentMap);
			//var storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
			//parms = storytellerComp.GenerateParms(def.category, parms.target);
			_ = mSendStandardLetter.Invoke(worker, new object[] { baseLetterLabel, baseLetterText, LetterDefOf.PositiveEvent, parms, (LookTargets)pawn, Array.Empty<NamedArgument>() });
		}
	}
}