using Harmony;
using Puppeteer.Core;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;
using static Harmony.AccessTools;

namespace Puppeteer
{
	public class PawnImage
	{
		readonly string name;
		public Texture2D imageTexture;
		public bool isNew;

		public PawnImage(Pawn pawn, Texture2D imageTexture)
		{
			name = pawn.Name.ToStringShort;
			this.imageTexture = imageTexture;
			isNew = true;
		}

		public bool Write()
		{
			if (isNew == false) return false;
			isNew = false;
			var encodedImage = imageTexture.EncodeToJPG(50);
			try
			{
				File.WriteAllBytes($"C:\\Users\\andre\\Desktop\\Pawns\\{name}.jpg", encodedImage);
			}
			catch (Exception)
			{
			}
			return true;
		}
	}

	public static class Renderer
	{
		const int imageSize = 256;
		public static float renderOffset = 0f;
		public static Vector3 RenderOffsetVector => new Vector3(renderOffset, 0f, 0f);
		public static CellRect fakeViewRect = CellRect.Empty;
		public static bool fakeZoom = false;
		public static readonly Dictionary<Pawn, PawnImage> pawnImages = new Dictionary<Pawn, PawnImage>();
		static readonly FieldRef<SubcameraDriver, Camera[]> subcamerasRef = FieldRefAccess<SubcameraDriver, Camera[]>("subcameras");

		public static byte[] GetPawnPortrait(Pawn pawn, int size)
		{
			var renderTexture = PortraitsCache.Get(pawn, new Vector2(size, size));
			var portrait = new Texture2D(size, size, TextureFormat.ARGB32, false);
			RenderTexture.active = renderTexture;
			portrait.ReadPixels(new Rect(0, 0, size, size), 0, 0);
			portrait.Apply();
			var data = portrait.EncodeToPNG();
			UnityEngine.Object.Destroy(portrait);
			return data;
		}

		public static void SetCamera(Camera camera, ref Vector3 position, float size)
		{
			camera.orthographicSize = size;
			camera.farClipPlane = 100f;
			camera.transform.position = position;
		}

		public static void GetPawnScreenRender(Pawn pawn, float radius)
		{
			if (pawnImages.TryGetValue(pawn, out var image) == false)
			{
				var imageTexture = new Texture2D(imageSize, imageSize, TextureFormat.RGB24, false);
				image = new PawnImage(pawn, imageTexture);
				pawnImages[pawn] = image;
			}

			var camera = Find.Camera;
			var rememberFarClipPlane = camera.farClipPlane;
			var rememberPosition = camera.transform.position;
			var rememberOrthographicSize = camera.orthographicSize;

			// var camera = ColonistCameraManager.Camera;
			var subCameras = subcamerasRef(Current.SubcameraDriver);

			var cameraPos = new Vector3(renderOffset + pawn.DrawPos.x, 40f, pawn.DrawPos.z);
			SetCamera(camera, ref cameraPos, radius);
			for (var i = 0; i < subCameras.Length; i++)
				SetCamera(subCameras[i], ref cameraPos, radius);

			var renderTexture = RenderTexture.GetTemporary(imageSize, imageSize, 24);
			camera.targetTexture = renderTexture;
			RenderTexture.active = renderTexture;
			camera.Render();
			image.imageTexture.ReadPixels(new Rect(0, 0, imageSize, imageSize), 0, 0, false);
			image.imageTexture.Apply();
			image.isNew = true;
			RenderTexture.ReleaseTemporary(renderTexture);
			camera.targetTexture = null;
			RenderTexture.active = null;

			SetCamera(camera, ref rememberPosition, rememberOrthographicSize);
			camera.farClipPlane = rememberFarClipPlane;
		}

		public static void RemovePawn(Pawn pawn)
		{
			if (pawnImages.TryGetValue(pawn, out var image))
			{
				UnityEngine.Object.Destroy(image.imageTexture);
				_ = pawnImages.Remove(pawn);
			}
		}
	}

	[HarmonyPatch(typeof(TickManager))]
	[HarmonyPatch(nameof(TickManager.TickManagerUpdate))]
	static class Verse_TickManager_TickManagerUpdate_Patch
	{
		static int throttle = 0;
		static int counter = 0;

		public static void Postfix()
		{
			if (++throttle % 15 != 0) return;
			var images = Renderer.pawnImages.Values.ToArray();
			if (images.Length == 0) return;
			if (++counter >= images.Length) counter = 0;
			_ = images[counter].Write();
		}
	}

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
	[HarmonyPatch("DrawMeshImpl")]
	static class Graphics_DrawMeshImpl_Patch
	{
		public static void Prefix(ref Matrix4x4 matrix)
		{
			if (Renderer.renderOffset == 0f) return;
			matrix = matrix.OffsetRef(new Vector3(Renderer.renderOffset, 0f, 0f));
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("DrawMeshInstancedImpl")]
	[HarmonyPatch(new[] { typeof(Mesh), typeof(int), typeof(Material), typeof(List<Matrix4x4>), typeof(MaterialPropertyBlock), typeof(ShadowCastingMode), typeof(bool), typeof(int), typeof(Camera) })]
	static class Graphics_DrawMeshInstancedImpl1_Patch
	{
		public static void Prefix(List<Matrix4x4> matrices)
		{
			if (Renderer.renderOffset == 0f) return;
			for (var i = 0; i < matrices.Count; i++)
				matrices[i] = matrices[i].Offset(Renderer.RenderOffsetVector);
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("DrawMeshInstancedImpl")]
	[HarmonyPatch(new[] { typeof(Mesh), typeof(int), typeof(Material), typeof(Matrix4x4[]), typeof(int), typeof(MaterialPropertyBlock), typeof(ShadowCastingMode), typeof(bool), typeof(int), typeof(Camera) })]
	static class Graphics_DrawMeshInstancedImpl2_Patch
	{
		public static void Prefix(Matrix4x4[] matrices)
		{
			if (Renderer.renderOffset == 0f) return;
			for (var i = 0; i < matrices.Length; i++)
				matrices[i] = matrices[i].Offset(Renderer.RenderOffsetVector);
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("DrawMeshInstancedIndirectImpl")]
	static class Graphics_DrawMeshInstancedIndirectImpl_Patch
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
			foreach (var map in Current.Game.Maps)
				if (WorldRendererUtility.WorldRenderedNow || map != Find.CurrentMap)
					Tools.RenderColonists(map, false);
		}
	}

	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.MapUpdate))]
	static class Map_MapUpdate_Patch
	{
		public static void Postfix()
		{
			if (WorldRendererUtility.WorldRenderedNow) return;
			var map = Find.CurrentMap;
			if (map != null) Tools.RenderColonists(map, true);
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

			// Renderer.GetPawnScreenRender(pawn, 1.5f);

			//var imageData = Renderer.GetPawnPortrait(pawn, 256);
			//File.WriteAllBytes($"C:\\Users\\andre\\Desktop\\Pawns\\Portrait-{pawn.Name.ToStringShort}.png", imageData);

			// Puppeteer.instance.PawnUpdate(pawn);
		}
	}
}