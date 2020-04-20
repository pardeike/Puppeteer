using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
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
			Controller.instance.SetEvent(Event.ColonistsChanged);
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
				Controller.PawnAvailable(__instance);
				Controller.instance.SetEvent(Event.ColonistsChanged);
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
				Controller.instance.SetEvent(Event.ColonistsChanged);
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
				Controller.instance.SetEvent(Event.ColonistsChanged);
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
				Controller.instance.SetEvent(Event.ColonistsChanged);
		}
	}

	[HarmonyPatch(typeof(ColonistBar))]
	[HarmonyPatch("Reorder")]
	static class ColonistBar_Reorder_Patch
	{
		public static void Postfix()
		{
			Controller.instance.SetEvent(Event.ColonistsChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_WorkSettings))]
	[HarmonyPatch(nameof(Pawn_WorkSettings.SetPriority))]
	static class Pawn_WorkSettings_SetPriority_Patch
	{
		public static void Postfix()
		{
			Controller.instance.SetEvent(Event.PrioritiesChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_WorkSettings))]
	[HarmonyPatch(nameof(Pawn_WorkSettings.Notify_UseWorkPrioritiesChanged))]
	static class Pawn_WorkSettings_Notify_UseWorkPrioritiesChanged_Patch
	{
		public static void Postfix()
		{
			Controller.instance.SetEvent(Event.PrioritiesChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_TimetableTracker))]
	[HarmonyPatch(nameof(Pawn_TimetableTracker.SetAssignment))]
	static class Pawn_TimetableTracker_SetPriority_Patch
	{
		public static void Postfix()
		{
			Controller.instance.SetEvent(Event.SchedulesChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_TimetableTracker), MethodType.Constructor)]
	[HarmonyPatch(new Type[] { typeof(Pawn) })]
	static class Pawn_TimetableTracker_Constructor_Patch
	{
		public static void Postfix()
		{
			Controller.instance.SetEvent(Event.SchedulesChanged);
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

	[HarmonyPatch(typeof(ColonistBarDrawLocsFinder))]
	[HarmonyPatch("GetDrawLoc")]
	static class ColonistBarDrawLocsFinder_GetDrawLoc_Patch
	{
		public static void Postfix(ref Vector2 __result, float scale)
		{
			__result.y += 15 * scale;
		}
	}
}