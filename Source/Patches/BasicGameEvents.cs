using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Puppeteer
{
	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch(nameof(Game.FinalizeInit))]
	static class Game_FinalizeInit_Patch
	{
		public static void Postfix()
		{
			Puppeteer.instance.SetEvent(Event.GameEntered);
		}
	}

	[HarmonyPatch(typeof(MainMenuDrawer))]
	[HarmonyPatch(nameof(MainMenuDrawer.Init))]
	static class MainMenuDrawer_Init_Patch
	{
		public static void Postfix()
		{
			Puppeteer.instance.SetEvent(Event.GameExited);
		}
	}

	[HarmonyPatch(typeof(Current))]
	[HarmonyPatch(nameof(Current.Notify_LoadedSceneChanged))]
	class Current_Notify_LoadedSceneChanged_Patch
	{
		static void Postfix()
		{
			if (GenScene.InPlayScene)
				RenderCamera.Create();
		}
	}

	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.MapUpdate))]
	static class Map_MapUpdate_Patch
	{
		public static void Prefix(Map __instance)
		{
			PlayerPawns.Update(__instance);
		}

		public static void Postfix()
		{
			Puppeteer.instance.SetEvent(Event.SendChangedPriorities);
			Puppeteer.instance.SetEvent(Event.SendChangedSchedules);
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
			Puppeteer.instance.SetEvent(Event.Save);
		}
	}
}