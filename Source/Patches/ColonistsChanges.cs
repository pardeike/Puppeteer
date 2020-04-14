using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using Verse;

namespace Puppeteer
{
	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.SpawnSetup))]
	static class Pawn_SpawnSetup_Patch
	{
		public static void Postfix(Pawn __instance, bool respawningAfterLoad)
		{
			if (respawningAfterLoad == false && __instance.Spawned && __instance.IsColonist)
				Puppeteer.instance.SetEvent(Event.ColonistsChanged);
		}
	}

	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.FinalizeLoading))]
	static class Map_FinalizeLoading_Patch
	{
		public static void Postfix()
		{
			Puppeteer.instance.SetEvent(Event.ColonistsChanged);
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
	[HarmonyPatch(nameof(Pawn.DeSpawn))]
	static class Pawn_DeSpawn_Patch
	{
		public static void Postfix(Pawn __instance)
		{
			if (__instance.IsColonist && Thing_SplitOff_Patch.inSplitOff == false)
			{
				Puppeteer.instance.PawnUnavailable(__instance);
				Puppeteer.instance.SetEvent(Event.ColonistsChanged);
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
				Puppeteer.instance.PawnUnavailable(__instance);
				Puppeteer.instance.SetEvent(Event.ColonistsChanged);
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
				Puppeteer.instance.SetEvent(Event.ColonistsChanged);
		}
	}

	[HarmonyPatch(typeof(ColonistBar))]
	[HarmonyPatch("Reorder")]
	static class ColonistBar_Reorder_Patch
	{
		public static void Postfix()
		{
			Puppeteer.instance.SetEvent(Event.ColonistsChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_WorkSettings))]
	[HarmonyPatch(nameof(Pawn_WorkSettings.SetPriority))]
	static class Pawn_WorkSettings_SetPriority_Patch
	{
		public static void Postfix()
		{
			Puppeteer.instance.SetEvent(Event.PrioritiesChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_WorkSettings))]
	[HarmonyPatch(nameof(Pawn_WorkSettings.Notify_UseWorkPrioritiesChanged))]
	static class Pawn_WorkSettings_Notify_UseWorkPrioritiesChanged_Patch
	{
		public static void Postfix()
		{
			Puppeteer.instance.SetEvent(Event.PrioritiesChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_TimetableTracker))]
	[HarmonyPatch(nameof(Pawn_TimetableTracker.SetAssignment))]
	static class Pawn_TimetableTracker_SetPriority_Patch
	{
		public static void Postfix()
		{
			Puppeteer.instance.SetEvent(Event.SchedulesChanged);
		}
	}

	[HarmonyPatch(typeof(Pawn_TimetableTracker), MethodType.Constructor)]
	[HarmonyPatch(new Type[] { typeof(Pawn) })]
	static class Pawn_TimetableTracker_Constructor_Patch
	{
		public static void Postfix()
		{
			Puppeteer.instance.SetEvent(Event.SchedulesChanged);
		}
	}
}