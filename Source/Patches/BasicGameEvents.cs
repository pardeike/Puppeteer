using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	[HarmonyPatch(typeof(Root_Play))]
	[HarmonyPatch(nameof(Root_Play.Start))]
	static class Root_Play_Start_Patch
	{
		public static void Postfix()
		{
			LongEventHandler.QueueLongEvent(delegate ()
			{
				if (MainMenuDrawer_Init_Patch.gameEntered)
					Controller.instance.SetEvent(PuppeteerEvent.GameExited);
				Controller.instance.SetEvent(PuppeteerEvent.GameEntered);
				MainMenuDrawer_Init_Patch.gameEntered = true;
			}, null, false, null, false);
		}
	}

	[HarmonyPatch(typeof(MainMenuDrawer))]
	[HarmonyPatch(nameof(MainMenuDrawer.Init))]
	static class MainMenuDrawer_Init_Patch
	{
		public static bool gameEntered = false;

		public static void Postfix()
		{
			if (gameEntered)
				Controller.instance.SetEvent(PuppeteerEvent.GameExited);
			gameEntered = false;
			VersionInformation.Show();
		}
	}

	[HarmonyPatch(typeof(LearningReadout))]
	[HarmonyPatch(nameof(LearningReadout.LearningReadoutOnGUI))]
	static class LearningReadout_WindowOnGUI_Patch
	{
		public static bool Prefix()
		{
			return PuppetCommentator.IsShowing == false;
		}
	}

	[HarmonyPatch(typeof(Prefs))]
	[HarmonyPatch(nameof(Prefs.DevMode), MethodType.Setter)]
	static class Prefs_DevMode_Patch
	{
		public static void Postfix()
		{
			Find.ColonistBar.MarkColonistsDirty();
		}
	}

	[HarmonyPatch(typeof(Root))]
	[HarmonyPatch(nameof(Root.OnGUI))]
	static class Root_OnGUI_Patch
	{
		static int firstTimeCounter = 120;

		public static void Postfix()
		{
			if (firstTimeCounter >= 0)
			{
				firstTimeCounter--;
				if (firstTimeCounter == 0)
					Tools.LogWarning("Hello");
			}

			PuppetCommentator.Update();
			OperationQueue.Process(OperationType.Log);
		}
	}

	[HarmonyPatch(typeof(GlobalControls))]
	[HarmonyPatch(nameof(GlobalControls.GlobalControlsOnGUI))]
	static class GlobalControls_GlobalControlsOnGUI_Patch
	{
		public static void Postfix()
		{
			if (Event.current.type != EventType.Layout)
				GeneralGUI.Update();
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
			Controller.instance.SetEvent(PuppeteerEvent.SendChangedPriorities);
			Controller.instance.SetEvent(PuppeteerEvent.SendChangedSchedules);
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
			Controller.instance.SetEvent(PuppeteerEvent.Save);
		}
	}
}