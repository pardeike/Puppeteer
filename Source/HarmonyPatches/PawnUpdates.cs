using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace Puppeteer
{
	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch(nameof(CameraDriver.CurrentViewRect), MethodType.Getter)]
	static class CameraDriver_CurrentViewRect_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static bool Prefix(ref CellRect __result)
		{
			if (Renderer.fakeViewRect.IsEmpty) return true;
			__result = Renderer.fakeViewRect;
			return false;
		}
	}

	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch(nameof(CameraDriver.CurrentZoom), MethodType.Getter)]
	static class CameraDriver_get_CurrentZoom_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static bool Prefix(ref CameraZoomRange __result)
		{
			if (Renderer.fakeZoom == false) return true;
			__result = CameraZoomRange.Closest;
			return false;
		}
	}

	/* TODO: do we need this?
	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.Center), MethodType.Getter)]
	static class Map_Center_Patch
	{
		public static void Postfix(ref IntVec3 __result)
		{
			if (Renderer.renderOffset == 0f) return;
			__result += new IntVec3((int)Renderer.renderOffset, 0, 0);
		}
	}*/

	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.Name), MethodType.Setter)]
	static class Pawn_set_Name_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix(Pawn __instance)
		{
			if (__instance.IsColonist)
				Controller.instance.SetEvent(PuppeteerEvent.ColonistsChanged);
		}
	}

	[HarmonyPatch]
	static class Area_Patches
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(AreaManager), "NotifyEveryoneAreaRemoved");
			yield return AccessTools.Method(typeof(AreaManager), "TryMakeNewAllowed");
			yield return AccessTools.Method(typeof(Area_Allowed), "SetLabel");
		}

		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			Controller.instance.SetEvent(PuppeteerEvent.AreasChanged);
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("Internal_DrawMesh")]
	static class Graphics_Internal_DrawMesh_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix(ref Matrix4x4 matrix)
		{
			if (Renderer.renderOffset == 0f) return;
			matrix = matrix.OffsetRef(new Vector3(Renderer.renderOffset, 0f, 0f));
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("DrawMeshInstanced")]
	[HarmonyPatch(new[] { typeof(Mesh), typeof(int), typeof(Material), typeof(Matrix4x4[]), typeof(int), typeof(MaterialPropertyBlock), typeof(ShadowCastingMode), typeof(bool), typeof(int), typeof(Camera), typeof(LightProbeUsage), typeof(LightProbeProxyVolume) })]
	static class Graphics_DrawMeshInstanced_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix(Matrix4x4[] matrices)
		{
			if (Renderer.renderOffset == 0f) return;
			for (var i = 0; i < matrices.Length; i++)
				matrices[i] = matrices[i].Offset(Renderer.RenderOffsetVector);
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("Internal_DrawMeshInstancedIndirect")]
	static class Graphics_Internal_DrawMeshInstancedIndirect_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix(ref Bounds bounds)
		{
			if (Renderer.renderOffset == 0f) return;
			bounds.center += Renderer.RenderOffsetVector;
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("Internal_DrawMeshNow1")]
	static class Graphics_Internal_DrawMeshNow1_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix(ref Vector3 position)
		{
			if (Renderer.renderOffset == 0f) return;
			position += Renderer.RenderOffsetVector;
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("Internal_DrawMeshNow2")]
	static class Graphics_Internal_DrawMeshNow2_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix(ref Matrix4x4 matrix)
		{
			if (Renderer.renderOffset == 0f) return;
			matrix = matrix.OffsetRef(Renderer.RenderOffsetVector);
		}
	}

	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch(nameof(Game.UpdatePlay))]
	static class Game_UpdatePlay_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			if (Event.current.type == EventType.Repaint)
			{
				OperationQueue.Process(OperationType.Portrait);
				OperationQueue.Process(OperationType.RenderMap);
				Controller.instance.SetEvent(PuppeteerEvent.UpdateSocials);
				OperationQueue.Process(OperationType.SocialRelations);
			}
			Controller.instance.SetEvent(PuppeteerEvent.UpdateColonists);
		}
	}


	[HarmonyPatch(typeof(WindowStack))]
	[HarmonyPatch(nameof(WindowStack.WindowStackOnGUI))]
	static class WindowStack_WindowStackOnGUI_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			if (Event.current.type != EventType.Repaint) return;

			OperationQueue.Process(OperationType.Select);
			Controller.instance.SetEvent(PuppeteerEvent.UpdateGear);
			OperationQueue.Process(OperationType.Gear);
		}
	}

	[HarmonyPatch(typeof(Widgets))]
	[HarmonyPatch(nameof(Widgets.WidgetsOnGUI))]
	static class Widgets_WidgetsOnGUI_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			OperationQueue.Process(OperationType.Job);
			OperationQueue.Process(OperationType.SetState);
		}
	}

	[HarmonyPatch(typeof(PortraitsCache))]
	[HarmonyPatch(nameof(PortraitsCache.SetDirty))]
	static class PortraitsCache_SetDirty_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix(Pawn pawn)
		{
			Controller.instance.UpdatePortrait(pawn);
		}
	}

	[HarmonyPatch(typeof(PawnRenderer))]
	[HarmonyPatch("RenderPawnInternal")]
	[HarmonyPatch(new Type[] { typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(bool), typeof(bool), typeof(bool) })]
	static class PawnRenderer_RenderPawnInternal_Patch
	{
		public static void Prefix(Pawn ___pawn)
		{
			if (State.pawnsToRefresh.Contains(___pawn))
			{
				_ = State.pawnsToRefresh.Remove(___pawn);
				___pawn.Drawer.renderer.graphics.ResolveAllGraphics();
				Controller.instance.UpdatePortrait(___pawn);
				PortraitsCache.SetDirty(___pawn);
			}
		}
	}

	[HarmonyPatch(typeof(PortraitsCache))]
	[HarmonyPatch("SetAnimatedPortraitsDirty")]
	static class ColonistBar_MarkColonistsDirty_Patch
	{
		static readonly List<Pawn> previousChangedPawns = new List<Pawn>();

		static void ObserveChanges(List<Pawn> changedPawns)
		{
			previousChangedPawns.DoIf(pawn => changedPawns.Contains(pawn) == false, pawn => Controller.instance.UpdatePortrait(pawn));
			previousChangedPawns.Clear(); // don't replace the lists directly
			previousChangedPawns.AddRange(changedPawns);
		}

		[HarmonyPriority(Priority.First)]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var m_ObserveChanges = SymbolExtensions.GetMethodInfo(() => ObserveChanges(null));
			var f_toSetDirty = AccessTools.Field(typeof(PortraitsCache), "toSetDirty");
			var m_get_Count = AccessTools.Property(typeof(List<Pawn>), "Count").GetGetMethod();
			var list = instructions.ToList();
			var found = false;
			for (var n = 0; n < list.Count; n++)
			{
				var instruction = list[n];
				if (instruction.LoadsField(f_toSetDirty) == false) continue;
				var nextInstruction = list[n + 1];
				if (nextInstruction.Calls(m_get_Count) == false) continue;
				list.InsertRange(n + 1, new[]
				{
					new CodeInstruction(OpCodes.Dup),
					new CodeInstruction(OpCodes.Call, m_ObserveChanges),
				});
				found = true;
				break;
			}
			if (found == false) Log.Error("Patching SetAnimatedPortraitsDirty: cannot find ldsfld toSetDirty followed by List<Pawn>.Count");
			return list.AsEnumerable();
		}
	}
}
