using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
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

	[HarmonyPatch]
	static class Pawn_DeSpawn_Kill_Patch
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.DeSpawn));
			yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill));
		}

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