using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class Renderer
	{
		public static float renderOffset = 0f;
		public static Vector3 RenderOffsetVector => new Vector3(renderOffset, 0f, 0f);
		public static CellRect fakeViewRect = CellRect.Empty;
		public static bool fakeZoom = false;
		public static AccessTools.FieldRef<bool> skipCustomRendering = null;

		static Renderer()
		{
			var t = AccessTools.TypeByName("CameraPlus.CameraPlusMain");
			if (t == null) return;
			var f = AccessTools.Field(t, "skipCustomRendering");
			skipCustomRendering = AccessTools.StaticFieldRefAccess<bool>(f);
		}

		public static byte[] GetPawnPortrait(Pawn pawn, Vector2 boundings)
		{
			var renderTexture = PortraitsCache.Get(pawn, boundings, new Vector3(0f, 0f, 0.11f), 1.28205f);
			var w = renderTexture.width;
			var h = renderTexture.height;
			var portrait = new Texture2D(w, h, TextureFormat.ARGB32, false);
			var active = RenderTexture.active;
			RenderTexture.active = renderTexture;
			portrait.ReadPixels(new Rect(0, 0, w, h), 0, 0);
			portrait.Apply();
			RenderTexture.active = active;
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

		public static Texture2D ScaleTexture(Texture2D texture, int size)
		{
			var renderTexture = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
			var active = RenderTexture.active;
			RenderTexture.active = renderTexture;
			Graphics.Blit(texture, renderTexture);
			var result = new Texture2D(size, size, TextureFormat.RGBA32, false);
			result.ReadPixels(new Rect(0, 0, size, size), 0, 0);
			RenderTexture.active = active;
			RenderTexture.ReleaseTemporary(renderTexture);
			return result;
		}

		public static byte[] GetImage(Texture2D texture, int size, int compression = 70)
		{
			var result = ScaleTexture(texture, size);
			var data = compression == 0 ? result.EncodeToPNG() : result.EncodeToJPG(compression);
			UnityEngine.Object.Destroy(result);
			return data;
		}

		public static byte[] GetImageMatrix(Texture2D[] textures, int size, bool disabled = false, int compression = 70)
		{
			var scaledTextures = textures.Select(tex => ScaleTexture(tex, size)).ToArray();
			for (var i = 0; i < scaledTextures.Length; i++)
				File.WriteAllBytes(@"C:\Users\andre\Desktop\Icons\" + (i + 1) + ".png", scaledTextures[i].EncodeToPNG());

			var count = textures.Length;
			var renderTexture = RenderTexture.GetTemporary(count * size, size, 0, RenderTextureFormat.ARGB32);

			var active = RenderTexture.active;
			RenderTexture.active = renderTexture;
			GL.PushMatrix();
			GL.LoadPixelMatrix(0, count * size, size, 0);
			var rect = new Rect(0, 0, size, size);
			for (var i = 0; i < count; i++)
			{
				//	Graphics.Blit(scaledTextures[i], renderTexture, Vector2.one, new Vector2(i * size, 0));
				var material = disabled ? TexUI.GrayscaleGUI : null;
				GenUI.DrawTextureWithMaterial(rect, Command.BGTex, material);
				Graphics.DrawTexture(rect, Command.BGTex, new Rect(0, 0, Command.BGTex.width, Command.BGTex.height), 0, 0, 0, 0, new Color(0.5f, 0.5f, 0.5f, 0.5f), material);
				Widgets.DrawTextureFitted(rect, scaledTextures[i], 0.85f, Vector2.one, new Rect(0f, 0f, 1f, 1f), 0f, material);

				rect.x += i * size;
			}
			GL.PopMatrix();

			var result = new Texture2D(count * size, size, TextureFormat.ARGB32, false);
			result.ReadPixels(new Rect(0, 0, count * size, size), 0, 0, false);

			RenderTexture.active = active;

			RenderTexture.ReleaseTemporary(renderTexture);
			var data = compression == 0 ? result.EncodeToPNG() : result.EncodeToJPG(compression);
			File.WriteAllBytes(@"C:\Users\andre\Desktop\Icons\gizmo.png", data);
			UnityEngine.Object.Destroy(result);
			return data;
		}

		public static byte[] GetCommandsMatrix(List<Command> commands)
		{
			var count = commands.Count;
			var size = 75;

			var renderTexture = RenderTexture.GetTemporary(count * size, size, 0, RenderTextureFormat.ARGB32);
			RenderTexture.active = renderTexture;

			GL.PushMatrix();
			GL.LoadPixelMatrix(0, count * size, size, 0);
			var rect = new Rect(0, 0, size, size);
			for (var i = 0; i < count; i++)
			{
				_ = commands[i].GizmoOnGUI(new Vector2(i * size, 0), 100000);
				rect.x += size;
			}
			GL.PopMatrix();

			var result = new Texture2D(count * size, size, TextureFormat.ARGB32, false);
			result.ReadPixels(new Rect(0, 0, count * size, size), 0, 0, false);
			result.Apply();
			RenderTexture.active = null;

			RenderTexture.ReleaseTemporary(renderTexture);

			var topLeftPixel = result.GetPixel(0, 0);
			if (topLeftPixel.r + topLeftPixel.g + topLeftPixel.b == 0)
			{
				UnityEngine.Object.Destroy(result);
				return null;
			}

			var data = result.EncodeToPNG();
			UnityEngine.Object.Destroy(result);
			return data;
		}

		public static Func<byte[]> GridRenderer(int[] grid)
		{
			if (grid == null) return () => Array.Empty<byte>();
			var x1 = grid[0];
			var z1 = grid[1];
			var x2 = grid[2] + 1;
			var z2 = grid[3] + 1;

			var camera = RenderCamera.camera;
			if (camera == null) return null;
			var dx = x2 - x1;
			var dz = z2 - z1;

			if (dx <= 0 || dz <= 0) return null;

			var centerX = (x1 + x2) / 2f;
			var centerZ = (z1 + z2) / 2f;

			var cameraPos = new Vector3(renderOffset + centerX, 40f, centerZ);
			SetCamera(camera, ref cameraPos, Math.Max(dx / 2f, dz / 2f));

			var f = GenMath.LerpDouble(1.5f, 8f, 1f, 4f, (float)Math.Sqrt(Math.Min(dx, dz)));
			var sizeX = (int)(Puppeteer.Settings.mapImageSize * f);
			var sizeZ = (int)(Puppeteer.Settings.mapImageSize * f * dz / dx);
			var renderTexture = RenderTexture.GetTemporary(sizeX, sizeZ, 24);
			camera.targetTexture = renderTexture;
			var active = RenderTexture.active;
			RenderTexture.active = renderTexture;
			camera.Render();
			var imageTexture = new Texture2D(sizeX, sizeZ, TextureFormat.RGB24, false);
			imageTexture.ReadPixels(new Rect(0, 0, sizeX, sizeZ), 0, 0, false);
			imageTexture.Apply();
			RenderTexture.ReleaseTemporary(renderTexture);
			camera.targetTexture = null;
			RenderTexture.active = active;

			var q_from = GenMath.LerpDouble(1, 9, 100, 65, Puppeteer.Settings.mapImageCompression);
			var q_to = GenMath.LerpDouble(1, 9, 80, 25, Puppeteer.Settings.mapImageCompression);
			var quality = (int)GenMath.LerpDoubleClamped(8, 40, q_from, q_to, Math.Min(dx, dz));
			return () =>
			{
				var jpgData = imageTexture.EncodeToJPG(quality);
				UnityEngine.Object.Destroy(imageTexture);
				return jpgData;
			};
		}

		private static void Render(Pawn pawn, CellRect viewRect, Action renderer)
		{
			var map = pawn?.Map;
			if (map == null) return;

			var savedMap = Find.CurrentMap;
			fakeZoom = true;
			Tools.SetCurrentMapDirectly(map);
			renderOffset = Tools.CurrentMapOffset();
			fakeViewRect = new CellRect(0, 0, map.Size.x, map.Size.z);

			// flickers with Z-Levels
			//map.skyManager.SkyManagerUpdate();

			map.powerNetManager.UpdatePowerNetsAndConnections_First();
			map.glowGrid.GlowGridUpdate_First();
			PlantFallColors.SetFallShaderGlobals(map);
			map.waterInfo.SetTextures();

			var pos = pawn.Position;
			fakeViewRect = viewRect.ContractedBy(-1);

			if (skipCustomRendering != null)
				skipCustomRendering() = true;

			map.mapDrawer.MapMeshDrawerUpdate_First();
			map.mapDrawer.DrawMapMesh();
			map.dynamicDrawManager.DrawDynamicThings();

			map.gameConditionManager.GameConditionManagerDraw(map);
			MapEdgeClipDrawer.DrawClippers(map);
			map.designationManager.DrawDesignations();
			map.overlayDrawer.DrawAllOverlays();

			renderer();

			if (skipCustomRendering != null)
				skipCustomRendering() = false;

			fakeViewRect = CellRect.Empty;
			renderOffset = 0f;
			Tools.SetCurrentMapDirectly(savedMap);
			fakeZoom = false;
		}

		public static void RenderMap(State.Puppeteer puppeteer, int[] grid)
		{
			var pawn = puppeteer?.puppet?.pawn;
			pawn = Tools.GetCarrier(pawn) ?? pawn;
			if (pawn == null || pawn.Spawned == false) return;

			var vID = puppeteer.vID;
			var px = pawn.Position.x;
			var pz = pawn.Position.z;
			var phx = pawn.DrawPos.x;
			var phz = pawn.DrawPos.z;

			const int initialRadius = 5;
			if (grid == null)
				grid = new int[] { px - initialRadius, pz - initialRadius, px + initialRadius, pz + initialRadius };

			OperationQueue.Add(OperationType.RenderMap, () =>
			{
				Render(pawn, new CellRect(grid[0], grid[1], grid[2] - grid[0], grid[3] - grid[1]), () =>
				{
					var compressionTask = GridRenderer(grid);
					BackgroundOperations.Add((connection) =>
					{
						var jpgData = compressionTask();
						connection.Send(new GridUpdate()
						{
							controller = vID,
							info = new GridUpdate.Info()
							{
								px = px,
								pz = pz,
								phx = phx,
								phz = phz,
								frame = new GridUpdate.Frame(grid),
								map = jpgData
							}
						});
					});
				});
			});
		}

		/*public static void RenderGizmos(Thing thing)
		{
			var inspectWindow = Find.WindowStack.Windows
				.OfType<MainTabWindow_Inspect>()
				.FirstOrDefault();
			if (inspectWindow == null) return;

			var selector = Find.Selector;
			var saved = selector.SelectedObjectsListForReading;
			selector.ClearSelection();
			selector.Select(thing, false, false);

			var gizmos = new List<Gizmo>();
			using (new GizmoCapture(gizmos))
				inspectWindow.DrawInspectGizmos();

			Find.Selector.ClearSelection();
			saved.ForEach(item => selector.Select(item, false, false));

			foreach (var gizmo in gizmos)
			{
				var result = gizmo.GizmoOnGUI(new Vector2(20000, 200000), GizmoGridDrawer.HeightDrawnRecently);
				gizmo.ProcessInput(Event.KeyboardEvent(" "));
			}
		}*/
	}

	/*[HarmonyPatch(typeof(GizmoGridDrawer))]
	[HarmonyPatch(nameof(GizmoGridDrawer.DrawGizmoGrid))]
	static class GizmoGridDrawer_DrawGizmoGrid_Patch
	{
		public static bool Prefix(IEnumerable<Gizmo> gizmos)
		{
			if (GizmoCapture.capture == false) return true;
			GizmoCapture.gizmos.AddRange(gizmos);
			return false;
		}
	}*/
}