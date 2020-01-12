using Harmony;
using Puppeteer.Core;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using static Puppeteer.Tools;

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
		public static float renderOffset = 0f;
		public static readonly Dictionary<Pawn, PawnImage> pawnImages = new Dictionary<Pawn, PawnImage>();

		public static byte[] GetPawnPortrait(Pawn pawn, int size)
		{
			var renderTexture = PortraitsCache.Get(pawn, new Vector2(size, size));
			var portrait = new Texture2D(size, size, TextureFormat.ARGB32, false);
			RenderTexture.active = renderTexture;
			portrait.ReadPixels(new Rect(0, 0, size, size), 0, 0);
			portrait.Apply();
			return portrait.EncodeToPNG();
		}

		public static void GetPawnScreenRender(Pawn pawn, int imageSize, float radius)
		{
			if (pawnImages.TryGetValue(pawn, out var image) == false)
			{
				var imageTexture = new Texture2D(imageSize, imageSize, TextureFormat.RGB24, false);
				image = new PawnImage(pawn, imageTexture);
				pawnImages[pawn] = image;
			}

			var startX = pawn.DrawPos.x - radius;
			var startZ = pawn.DrawPos.z - radius;
			var endX = startX + 2 * radius;
			var endZ = startZ + 2 * radius;

			var orthographicSize = 2 * radius;
			var cameraBaseY = 10f + 15f + (orthographicSize - 11f) / 49f * 50f;

			var camRectMinX = (int)Math.Floor(startX);
			var camRectMinZ = (int)Math.Floor(startZ);
			var camRectMaxX = (int)Math.Ceiling(endX);
			var camRectMaxZ = (int)Math.Ceiling(endZ);

			var camera = ColonistCameraManager.Camera;
			camera.orthographicSize = orthographicSize;
			camera.farClipPlane = cameraBaseY + 100f;
			camera.transform.position = new Vector3(Renderer.renderOffset + pawn.DrawPos.x, cameraBaseY, pawn.DrawPos.z);

			var renderTexture = RenderTexture.GetTemporary(imageSize, imageSize, 24);
			camera.targetTexture = renderTexture;
			RenderTexture.active = renderTexture;
			camera.Render();
			image.imageTexture.ReadPixels(new Rect(0, 0, imageSize, imageSize), 0, 0, false);
			image.isNew = true;
			RenderTexture.ReleaseTemporary(renderTexture);
			camera.targetTexture = null;
			RenderTexture.active = null;
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

	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.Center), MethodType.Getter)]
	static class Map_Center_Patch
	{
		public static void Postfix(ref IntVec3 __result)
		{
			if (Renderer.renderOffset != 0f)
				__result += new IntVec3((int)Renderer.renderOffset, 0, 0);
		}
	}

	[HarmonyPatch(typeof(GenThing))]
	[HarmonyPatch(nameof(GenThing.TrueCenter))]
	[HarmonyPatch(new Type[] { typeof(IntVec3), typeof(Rot4), typeof(IntVec2), typeof(float) })]
	static class GenThing_TrueCenter_Patch
	{
		public static void Postfix(ref Vector3 __result)
		{
			if (Renderer.renderOffset != 0f)
				__result += new Vector3(Renderer.renderOffset, 0f, 0f);
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("DrawMeshImpl")]
	static class Graphics_DrawMeshImpl_Patch
	{
		static Vector3 ExtractTranslationFromMatrix(ref Matrix4x4 matrix)
		{
			Vector3 translate;
			translate.x = matrix.m03;
			translate.y = matrix.m13;
			translate.z = matrix.m23;
			return translate;
		}

		static Quaternion ExtractRotationFromMatrix(ref Matrix4x4 matrix)
		{
			Vector3 forward;
			forward.x = matrix.m02;
			forward.y = matrix.m12;
			forward.z = matrix.m22;

			Vector3 upwards;
			upwards.x = matrix.m01;
			upwards.y = matrix.m11;
			upwards.z = matrix.m21;

			return Quaternion.LookRotation(forward, upwards);
		}

		static Vector3 ExtractScaleFromMatrix(ref Matrix4x4 matrix)
		{
			Vector3 scale;
			scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
			scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
			scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
			return scale;
		}

		public static void Prefix(ref Matrix4x4 matrix)
		{
			if (Renderer.renderOffset == 0f) return;
			matrix = Matrix4x4.TRS(
				ExtractTranslationFromMatrix(ref matrix) + new Vector3(Renderer.renderOffset, 0f, 0f),
				ExtractRotationFromMatrix(ref matrix),
				ExtractScaleFromMatrix(ref matrix)
			);
		}
	}

	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.MapUpdate))]
	static class Map_MapUpdate_Patch
	{
		static readonly Dictionary<Map, int> counters = new Dictionary<Map, int>();
		static readonly StaticFieldRef<int> lastViewRectGetFrameRef = StaticFieldRefAccess<int>(typeof(CameraDriver), "lastViewRectGetFrame");
		static readonly StaticFieldRef<CellRect> lastViewRectRef = StaticFieldRefAccess<CellRect>(typeof(CameraDriver), "lastViewRect");

		static void SetCurrentMapDirectly(Map map)
		{
			var game = Current.Game;
			game.currentMapIndex = (sbyte)game.Maps.IndexOf(map);
		}

		public static void Postfix(Map __instance)
		{
			var map = __instance;
			if (WorldRendererUtility.WorldRenderedNow == false && map == Find.CurrentMap)
			{
				map.mapPawns.FreeColonists.Do(colonist => Renderer.GetPawnScreenRender(colonist, 256, 1f));
				return;
			}

			var rememberedMap = Find.CurrentMap;
			SetCurrentMapDirectly(map);

			Renderer.renderOffset = 2000f * (1 + Current.Game.currentMapIndex);
			var cameraTransformPosition = Find.Camera.transform.position;
			Find.Camera.transform.position += new Vector3(Renderer.renderOffset, 0f, 0f);

			var rememberLastViewRectGetFrame = lastViewRectGetFrameRef();
			var rememberLastViewRect = lastViewRectRef();

			map.mapPawns.FreeColonists.Do(colonist =>
			{
				lastViewRectGetFrameRef() = Time.frameCount;
				lastViewRectRef() = new CellRect(colonist.Position.x - 2, colonist.Position.z - 2, colonist.Position.x + 2, colonist.Position.z + 2);

				//map.mapDrawer.DrawMapMesh();
				var sections = new HashSet<Section>();
				_ = sections.Add(map.mapDrawer.SectionAt(colonist.Position + new IntVec3(-1, 0, -1)));
				_ = sections.Add(map.mapDrawer.SectionAt(colonist.Position + new IntVec3(1, 0, -1)));
				_ = sections.Add(map.mapDrawer.SectionAt(colonist.Position + new IntVec3(1, 0, 1)));
				_ = sections.Add(map.mapDrawer.SectionAt(colonist.Position + new IntVec3(-1, 0, 1)));
				sections.ToList().Do(section => section.DrawSection(false));

				map.dynamicDrawManager.DrawDynamicThings();
				MapEdgeClipDrawer.DrawClippers(map);
				map.overlayDrawer.DrawAllOverlays();
				try { map.areaManager.AreaManagerUpdate(); }
				catch (Exception) { }
				Renderer.GetPawnScreenRender(colonist, 256, 1f);
			});

			lastViewRectGetFrameRef() = rememberLastViewRectGetFrame;
			lastViewRectRef() = rememberLastViewRect;

			Find.Camera.transform.position = cameraTransformPosition;
			SetCurrentMapDirectly(rememberedMap);
			Renderer.renderOffset = 0f;
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

			// Renderer.GetPawnScreenRender(pawn, 256, 1f);

			//var imageData = Renderer.GetPawnPortrait(pawn, 256);
			//File.WriteAllBytes($"C:\\Users\\andre\\Desktop\\Pawns\\Portrait-{pawn.Name.ToStringShort}.png", imageData);

			// Puppeteer.instance.PawnUpdate(pawn);
		}
	}
}