using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch(nameof(Game.LoadGame))]
	static class Game_LoadGame_Patch
	{
		public static void Postfix()
		{
			Controller.instance.SetEvent(PuppeteerEvent.ColonistsChanged);
			Tools.GameInit();
		}
	}

	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.SplitOff))]
	static class Thing_SplitOff_Patch
	{
		public static bool inSplitOff = false;

		static void Prefix()
		{
			inSplitOff = true;
		}

		static void Postfix()
		{
			inSplitOff = false;
		}
	}

	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.SpawnSetup))]
	static class Pawn_SpawnSetup_Patch
	{
		public static void Postfix(Pawn __instance, bool respawningAfterLoad)
		{
			if (respawningAfterLoad == false && __instance.Spawned && __instance.IsColonist)
			{
				Controller.instance.PawnAvailable(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.DeSpawn))]
	static class Pawn_DeSpawn_Patch
	{
		public static void Postfix(Pawn __instance)
		{
			if (__instance.IsColonist && Thing_SplitOff_Patch.inSplitOff == false)
			{
				Controller.instance.PawnUnavailable(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.Kill))]
	static class Pawn_Kill_Patch
	{
		public static void Postfix(Pawn __instance)
		{
			if (__instance.IsColonist)
			{
				Controller.instance.PawnUnavailable(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(WorldPawns))]
	[HarmonyPatch(nameof(WorldPawns.PassToWorld))]
	static class WorldPawns_PassToWorld_Patch
	{
		public static void Postfix(Pawn pawn)
		{
			if (pawn.IsColonist)
				Controller.instance.SetEvent(PuppeteerEvent.ColonistsChanged);
		}
	}

	[HarmonyPatch(typeof(ColonistBar))]
	[HarmonyPatch("Reorder")]
	static class ColonistBar_Reorder_Patch
	{
		public static void Postfix()
		{
			Controller.instance.SetEvent(PuppeteerEvent.ColonistsChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_WorkSettings))]
	[HarmonyPatch(nameof(Pawn_WorkSettings.SetPriority))]
	static class Pawn_WorkSettings_SetPriority_Patch
	{
		public static void Postfix()
		{
			Controller.instance.SetEvent(PuppeteerEvent.PrioritiesChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_WorkSettings))]
	[HarmonyPatch(nameof(Pawn_WorkSettings.Notify_UseWorkPrioritiesChanged))]
	static class Pawn_WorkSettings_Notify_UseWorkPrioritiesChanged_Patch
	{
		public static void Postfix()
		{
			Controller.instance.SetEvent(PuppeteerEvent.PrioritiesChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_TimetableTracker))]
	[HarmonyPatch(nameof(Pawn_TimetableTracker.SetAssignment))]
	static class Pawn_TimetableTracker_SetPriority_Patch
	{
		public static void Postfix()
		{
			Controller.instance.SetEvent(PuppeteerEvent.SchedulesChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_TimetableTracker), MethodType.Constructor)]
	[HarmonyPatch(new Type[] { typeof(Pawn) })]
	static class Pawn_TimetableTracker_Constructor_Patch
	{
		public static void Postfix()
		{
			Controller.instance.SetEvent(PuppeteerEvent.SchedulesChanged);
		}
	}

	[HarmonyPatch(typeof(ColonistBarColonistDrawer))]
	[HarmonyPatch(nameof(ColonistBarColonistDrawer.DrawColonist))]
	static class ColonistBarColonistDrawer_DrawColonist_Patch
	{
		public static void Prefix(Rect rect, Pawn colonist)
		{
			Drawing.DrawAssignmentStatus(colonist, rect);
		}
	}

	[HarmonyPatch(typeof(ColonistBarColonistDrawer))]
	[HarmonyPatch(nameof(ColonistBarColonistDrawer.HandleClicks))]
	static class ColonistBarColonistDrawer_HandleClicks_Patch
	{
		public static void Prefix(Rect rect, Pawn colonist)
		{
			Drawing.AssignFloatMenu(colonist, rect);
		}
	}

	[HarmonyPatch(typeof(ColonistBarDrawLocsFinder))]
	[HarmonyPatch("GetDrawLoc")]
	static class ColonistBarDrawLocsFinder_GetDrawLoc_Patch
	{
		const float BaseSpaceBetweenColonistsVertical = 32f;
		const float extraVerticalOffset = 15f;

		public static void Postfix(ref Vector2 __result, float scale)
		{
			__result.y += extraVerticalOffset * scale;
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instr in instructions)
			{
				var scale = Find.UIRoot == null || Find.MapUI == null ? 1f : (Find.ColonistBar?.Scale ?? 1f);
				if (instr.OperandIs(BaseSpaceBetweenColonistsVertical))
					instr.operand = BaseSpaceBetweenColonistsVertical + extraVerticalOffset * scale;
				yield return instr;
			}
		}
	}
}