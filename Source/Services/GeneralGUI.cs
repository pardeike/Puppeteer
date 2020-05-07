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
			var rect = new Rect(UI.screenWidth - width - padding, padding, width, height);
			GUI.color = Color.white;
			tex1.Draw(rect, true);

			rect.xMin += 2;
			rect.yMin += 8;
			rect.height = 9;
			rect.width = 45;
			var barRect = rect;

			GUI.color = new Color(f, 1 - f, 0);
			rect.width *= f;
			GUI.DrawTexture(rect.Rounded(), BaseContent.WhiteTex);

			RenderNumber(barRect, OutgoingRequests.AverageSendTime, true, TextAlignment.Left);
			RenderNumber(barRect, BackgroundOperations.Count, false, TextAlignment.Right);

			var tex2 = Assets.colonist;
			width = tex2.width / 2;
			height = tex2.height / 2;
			rect = new Rect(rect.x - 2 * padding - width, rect.center.y - height / 2, width, height);
			if (Widgets.ButtonImage(rect, Assets.colonist))
				UnassignedViewersMenu();

			GUI.color = savedColor;
		}

		static void RenderNumber(Rect rect, long val, bool useMilliseconds, TextAlignment direction)
		{
			rect.x = direction == TextAlignment.Left ? rect.x + 2 : rect.xMax;
			GUI.color = Color.white;
			var characters = val.ToString() + (useMilliseconds ? "$" : "");
			foreach (var c in characters)
			{
				var numTex = c == '$' ? Assets.numbers[10] : Assets.numbers[c - '0'];
				rect.width = numTex.width / 2f;
				if (direction == TextAlignment.Right) rect.x -= rect.width;
				GUI.DrawTexture(rect, numTex);
				rect.x += direction == TextAlignment.Left ? rect.width + 1 : -1;
			}
		}

		static void UnassignedViewersMenu()
		{
			var connectedViewers = State.Instance.ConnectedPuppeteers().Select(p => p.vID).OrderBy(vID => vID.name).ToList();
			if (connectedViewers.Any())
			{
				var list = new List<FloatMenuOption>();
				foreach (var vID in connectedViewers)
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
			Traverse.Create(Find.Scenario).Field("parts").GetValue<List<ScenPart>>().Clear();
			Find.WindowStack.Add(new Page_ConfigureStartingPawns()
			{
				next = null,
				nextAct = () => { CreateColonist(vID, Find.GameInitData.startingAndOptionalPawns[0]); }
			});
		}

		static void CreateColonist(ViewerID vID, Pawn pawn)
		{
			var map = Find.CurrentMap;
			if (CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out var cell) == false) return;

			_ = GenSpawn.Spawn(pawn, cell, map, WipeMode.Vanish);
			ShowWandererJoinedLetter(pawn);
			Controller.instance.AssignViewerToPawn(vID, pawn);

			var things = Find.Scenario.AllParts
				.SelectMany(part => part.PlayerStartingThings())
				.Select(thing =>
				{
					if (thing.def.CanHaveFaction)
						thing.SetFactionDirect(Faction.OfPlayer);
					return thing;
				})
				.ToList();

			foreach (var thing in things)
			{
				if (thing.def.IsWeapon && thing is ThingWithComps weapon)
				{
					if (EquipmentUtility.CanEquip(weapon, pawn))
						pawn.equipment.AddEquipment(weapon);
				}
				else if (thing is Apparel apparel)
				{
					if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
						pawn.apparel.Wear(apparel, false);
				}
			}
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