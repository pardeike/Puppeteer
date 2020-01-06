using Harmony;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Puppeteer
{
	class PuppeteerMain : Mod
	{
		public static Puppeteer puppeteer;

		public PuppeteerMain(ModContentPack content) : base(content)
		{
			var harmony = HarmonyInstance.Create("net.pardeike.harmony.Puppeteer");
			harmony.PatchAll();
		}
	}

	class Patches
	{
		[HarmonyPatch(typeof(Map))]
		[HarmonyPatch(nameof(Map.FinalizeInit))]
		static class Map_FinalizeInit_Patch
		{
			public static void Postfix()
			{
				if (PuppeteerMain.puppeteer == null)
					PuppeteerMain.puppeteer = new Puppeteer();
				PuppeteerMain.puppeteer.SetEvent(Event.GameEntered);
			}
		}

		[HarmonyPatch(typeof(MainMenuDrawer))]
		[HarmonyPatch(nameof(MainMenuDrawer.Init))]
		static class MainMenuDrawer_Init_Patch
		{
			public static void Postfix()
			{
				if (PuppeteerMain.puppeteer == null)
					PuppeteerMain.puppeteer = new Puppeteer();
				PuppeteerMain.puppeteer.SetEvent(Event.GameExited);
			}
		}

		[HarmonyPatch]
		static class GameDataSaveLoader_SaveGame_Patch
		{
			public static IEnumerable<MethodBase> TargetMethods()
			{
				yield return SymbolExtensions.GetMethodInfo(() => GameDataSaveLoader.SaveGame(""));
				yield return SymbolExtensions.GetMethodInfo(() => Root.Shutdown());
				yield return SymbolExtensions.GetMethodInfo(() => GenScene.GoToMainMenu());
			}

			public static void Postfix()
			{
				if (PuppeteerMain.puppeteer == null)
					PuppeteerMain.puppeteer = new Puppeteer();
				PuppeteerMain.puppeteer.SetEvent(Event.Save);
			}
		}

		[HarmonyPatch(typeof(ColonistBar))]
		[HarmonyPatch("CheckRecacheEntries")]
		static class ColonistBar_CheckRecacheEntries_Patch
		{
			public static void Prefix(bool ___entriesDirty)
			{
				if (___entriesDirty)
				{
					if (PuppeteerMain.puppeteer == null)
						PuppeteerMain.puppeteer = new Puppeteer();
					PuppeteerMain.puppeteer.SetEvent(Event.ColonistsChanged);
				}
			}
		}

		[HarmonyPatch(typeof(Thing))]
		[HarmonyPatch(nameof(Thing.Position), MethodType.Setter)]
		static class Thing_Position_Patch
		{
			public static void Postfix(Thing __instance)
			{
				var pawn = __instance as Pawn;
				if (pawn == null || pawn.Spawned == false || pawn.IsColonist == false)
					return;

				PuppeteerMain.puppeteer.PawnUpdate(pawn);
			}
		}
	}
}