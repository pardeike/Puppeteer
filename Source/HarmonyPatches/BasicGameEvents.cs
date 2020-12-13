using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	[HarmonyPatch(typeof(Root_Play))]
	[HarmonyPatch(nameof(Root_Play.Start))]
	static class Root_Play_Start_Patch
	{
		[HarmonyPriority(Priority.First)]
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

		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			if (gameEntered)
				Controller.instance.SetEvent(PuppeteerEvent.GameExited);
			gameEntered = false;
			VersionInformation.Show();
		}
	}

	[HarmonyPatch(typeof(PlaySettings))]
	[HarmonyPatch(nameof(PlaySettings.DoPlaySettingsGlobalControls))]
	static class PlaySettings_DoPlaySettingsGlobalControls_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix(WidgetRow row, bool worldView)
		{
			if (worldView) return;
			var old = PuppeteerMod.Settings.showOffLimitZones;
			row.ToggleableIcon(ref PuppeteerMod.Settings.showOffLimitZones, Assets.ShowOffLimits, "Toggle Off Limits", SoundDefOf.Mouseover_ButtonToggle, null);
			if (PuppeteerMod.Settings.showOffLimitZones != old)
				PuppeteerMod.SaveSettings();
		}
	}

	[HarmonyPatch(typeof(FloatMenuOption), MethodType.Constructor)]
	[HarmonyPatch(new[] { typeof(string), typeof(Action), typeof(MenuOptionPriority), typeof(Action), typeof(Thing), typeof(float), typeof(Func<Rect, bool>), typeof(WorldObject) })]
	static class FloatMenuOption_Constructor_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix(string label, Action action)
		{
			if (action == null || label == null) return;
			var idx = label.IndexOf(" (");
			if (idx > 0) label = label.Remove(idx);
			if (PuppeteerMod.Settings.menuCommands.Add(label))
				PuppeteerMod.SaveSettings();
		}
	}

	[HarmonyPatch(typeof(LearningReadout))]
	[HarmonyPatch(nameof(LearningReadout.LearningReadoutOnGUI))]
	static class LearningReadout_WindowOnGUI_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static bool Prefix()
		{
			return PuppetCommentator.IsShowing == false;
		}
	}

	[HarmonyPatch(typeof(Prefs))]
	[HarmonyPatch(nameof(Prefs.DevMode), MethodType.Setter)]
	static class Prefs_DevMode_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			if (Current.Game != null)
				Find.ColonistBar?.MarkColonistsDirty();
		}
	}

	[HarmonyPatch(typeof(Root))]
	[HarmonyPatch(nameof(Root.OnGUI))]
	static class Root_OnGUI_Patch
	{
		static int firstTimeCounter = 120;

		[HarmonyPriority(Priority.First)]
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
		[HarmonyPriority(Priority.First)]
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
		[HarmonyPriority(Priority.First)]
		static void Postfix()
		{
			if (GenScene.InPlayScene)
			{
				Controller.instance.SetEvent(PuppeteerEvent.MapEntered);
				RenderCamera.Create();
			}
		}
	}

	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.MapUpdate))]
	static class Map_MapUpdate_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix(Map __instance)
		{
			PlayerPawns.Update(__instance);
		}

		[HarmonyPriority(Priority.First)]
		public static void Postfix(Map __instance)
		{
			Controller.instance.SetEvent(PuppeteerEvent.SendChangedPriorities);
			Controller.instance.SetEvent(PuppeteerEvent.SendChangedSchedules);

			var offLimits = __instance.GetComponent<OffLimitsComponent>();
			offLimits.areas.Do(area =>
			{
				area.Drawer.MarkForDraw();
				area.Drawer.CellBoolDrawerUpdate();
			});
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

		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			Controller.instance.SetEvent(PuppeteerEvent.Save);
		}
	}

	[HarmonyPatch]
	static class TickManager_TimeSpeed_Patches
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Property(typeof(TickManager), nameof(TickManager.CurTimeSpeed)).GetSetMethod();
			yield return SymbolExtensions.GetMethodInfo(() => new TickManager().TogglePaused());
		}

		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			Controller.instance.SetEvent(PuppeteerEvent.TimeChanged);
		}
	}

	[HarmonyPatch(typeof(GizmoGridDrawer))]
	[HarmonyPatch(nameof(GizmoGridDrawer.DrawGizmoGrid))]
	static class GizmoGridDrawer_DrawGizmoGrid_Patch
	{
		static Command_Action CreateDeleteResurrectionPortal(ResurrectionPortal portal)
		{
			var h = (portal.created + GenDate.TicksPerDay - Find.TickManager.TicksGame + GenDate.TicksPerHour - 1) / GenDate.TicksPerHour;
			var hours = $"{h} hour" + (h != 1 ? "s" : "");
			return new Command_Action
			{
				defaultLabel = "Remove",
				icon = ContentFinder<Texture2D>.Get("RemoveResurrectionPortal", true),
				disabled = h > 0,
				disabledReason = "You have to wait " + hours + " to remove the portal",
				defaultDesc = "Removes the resurrection portal so you can build it somewhere else",
				order = -20f,
				action = () => portal.Destroy()
			};
		}

		[HarmonyPriority(Priority.First)]
		public static void Prefix(ref IEnumerable<Gizmo> gizmos)
		{
			if (!(Find.Selector.SelectedObjects.FirstOrDefault() is ResurrectionPortal portal)) return;
			gizmos = new List<Gizmo>() { CreateDeleteResurrectionPortal(portal) }.AsEnumerable();
		}
	}

	[HarmonyPatch()]
	static class PlayerIssuedOrders_Patch
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders");
			yield return AccessTools.Method(typeof(FloatMenuMakerMap), "AddDraftedOrders");
			yield return AccessTools.Method(typeof(FloatMenuMakerMap), "AddUndraftedOrders");
			// yield return AccessTools.Method(typeof(FloatMenuMakerMap), "AddJobGiverWorkOrders");
		}

		[HarmonyPriority(Priority.Last)]
		public static void Postfix(Pawn pawn, List<FloatMenuOption> opts)
		{
			void MarkOverwritten(FloatMenuOption opt)
			{
				var puppet = State.Instance.PuppetForPawn(pawn);
				if (puppet?.puppeteer != null)
				{
					// Log.Warning($"Cooldown for {pawn.LabelCap} [{opt.Label}]");
					puppet.lastPlayerCommand = Find.TickManager.TicksAbs;
				}
			}

			foreach (var opt in opts)
			{
				var savedAction = opt.action;
				if (savedAction != null)
					opt.action = () => { MarkOverwritten(opt); savedAction(); };
			}
		}
	}

	[HarmonyPatch(typeof(Pawn_DraftController))]
	[HarmonyPatch(nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
	static class Pawn_DraftController_Drafted_Patch
	{
		[HarmonyPriority(Priority.Last)]
		public static void Postfix(/*bool value,*/ Pawn ___pawn)
		{
			if (Tools.IsFakeDrafting) return;

			var puppet = State.Instance.PuppetForPawn(___pawn);
			if (puppet?.puppeteer != null)
			{
				// Log.Warning($"Cooldown for {___pawn.LabelCap} [{(value ? "Drafted" : "Undrafted")}]");
				puppet.lastPlayerCommand = Find.TickManager.TicksAbs;
			}
		}
	}

	[HarmonyPatch(typeof(DebugToolsPawns))]
	[HarmonyPatch("TryJobGiver")]
	static class DebugToolsPawns_TryJobGiver_Patch
	{
		[HarmonyPriority(Priority.Last)]
		public static void Postfix(Pawn p)
		{
			var puppet = State.Instance.PuppetForPawn(p);
			if (puppet?.puppeteer != null)
			{
				// Log.Warning($"Cooldown for {p.LabelCap} [Debug command]");
				puppet.lastPlayerCommand = Find.TickManager.TicksAbs;
			}
		}
	}

	[HarmonyPatch]
	static class Pawn_ThingOwner_Notifications_Patch
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(ThingOwner), "NotifyAdded");
			yield return AccessTools.Method(typeof(ThingOwner), "NotifyRemoved");
		}

		[HarmonyPriority(Priority.Last)]
		[HarmonyDebug]
		public static void Postfix(IThingHolder ___owner)
		{
			if (___owner is Pawn_ApparelTracker apparel)
			{
				Controller.instance.UpdateGear(apparel.pawn);
				return;
			}
			if (___owner is Pawn_InventoryTracker inventory)
			{
				Controller.instance.UpdateInventory(inventory.pawn);
				return;
			}
			if (___owner is Pawn_EquipmentTracker equipment)
			{
				Controller.instance.UpdateInventory(equipment.pawn);
				return;
			}
		}
	}
}
