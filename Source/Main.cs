using Harmony;
using RimWorld;
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
				PuppeteerMain.puppeteer.SetEvent(Event.Save);
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