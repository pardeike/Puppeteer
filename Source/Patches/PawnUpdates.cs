using HarmonyLib;
using Puppeteer.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace Puppeteer
{
	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch(nameof(CameraDriver.CurrentViewRect), MethodType.Getter)]
	static class CameraDriver_CurrentViewRect_Patch
	{
		public static bool Prefix(ref CellRect __result)
		{
			if (Renderer.fakeViewRect.IsEmpty) return true;
			__result = Renderer.fakeViewRect;
			return false;
		}
	}

	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch(nameof(CameraDriver.CurrentZoom), MethodType.Getter)]
	static class CameraDriver_CurrentZoom_Patch
	{
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

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("Internal_DrawMesh")]
	static class Graphics_Internal_DrawMesh_Patch
	{
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
		public static void Postfix()
		{
			Tools.RenderColonists(null);
			OperationQueue.Process(OperationType.Portrait);
		}
	}

	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.MapUpdate))]
	static class Map_MapUpdate_Patch
	{
		public static void Postfix(Map __instance)
		{
			var map = Find.CurrentMap;
			if (map != null) Tools.RenderColonists(__instance);
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

			// Puppeteer.instance.PawnUpdate(pawn);
		}
	}
}