using Harmony;
using RimWorld;
using RimWorld.Planet;
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
				PuppeteerMain.puppeteer.SetEvent(Event.ColonistsChanged);
		}
	}

	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.FinalizeLoading))]
	static class Map_FinalizeLoading_Patch
	{
		public static void Postfix()
		{
			PuppeteerMain.puppeteer.SetEvent(Event.ColonistsChanged);
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
				PuppeteerMain.puppeteer.SetEvent(Event.ColonistsChanged);
		}
	}

	[HarmonyPatch(typeof(WorldPawns))]
	[HarmonyPatch(nameof(WorldPawns.PassToWorld))]
	static class WorldPawns_PassToWorld_Patch
	{
		public static void Postfix(Pawn pawn)
		{
			if (pawn.IsColonist)
				PuppeteerMain.puppeteer.SetEvent(Event.ColonistsChanged);
		}
	}

	[HarmonyPatch(typeof(ColonistBar))]
	[HarmonyPatch("Reorder")]
	static class ColonistBar_Reorder_Patch
	{
		public static void Postfix()
		{
			PuppeteerMain.puppeteer.SetEvent(Event.ColonistsChanged);
		}
	}
}