using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class Renderer
	{
		public static float renderOffset = 0f;
		public static Vector3 RenderOffsetVector => new Vector3(renderOffset, 0f, 0f);
		public static CellRect fakeViewRect = CellRect.Empty;
		public static bool fakeZoom = false;

		public static byte[] GetPawnPortrait(Pawn pawn, Vector2 boundings)
		{
			var renderTexture = PortraitsCache.Get(pawn, boundings, new Vector3(0f, 0f, 0.11f), 1.28205f);
			var w = renderTexture.width;
			var h = renderTexture.height;
			var portrait = new Texture2D(w, h, TextureFormat.ARGB32, false);
			RenderTexture.active = renderTexture;
			portrait.ReadPixels(new Rect(0, 0, w, h), 0, 0);
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
			RenderTexture.active = renderTexture;
			camera.Render();
			var imageTexture = new Texture2D(sizeX, sizeZ, TextureFormat.RGB24, false);
			imageTexture.ReadPixels(new Rect(0, 0, sizeX, sizeZ), 0, 0, false);
			imageTexture.Apply();
			RenderTexture.ReleaseTemporary(renderTexture);
			camera.targetTexture = null;
			RenderTexture.active = null;

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

			// TODO: right place to call this?
			//       does it cause flickering?
			//map.weatherManager.DrawAllWeather();

			// TODO: needed?
			//map.glowGrid.MarkGlowGridDirty(pawn.Position);

			map.skyManager.SkyManagerUpdate();
			map.powerNetManager.UpdatePowerNetsAndConnections_First();
			map.glowGrid.GlowGridUpdate_First();

			PlantFallColors.SetFallShaderGlobals(map);

			// TODO: expensive?
			//map.waterInfo.SetTextures();

			var pos = pawn.Position;
			fakeViewRect = viewRect;

			map.mapDrawer.MapMeshDrawerUpdate_First();
			map.mapDrawer.DrawMapMesh();
			map.dynamicDrawManager.DrawDynamicThings();

			map.gameConditionManager.GameConditionManagerDraw(map);
			MapEdgeClipDrawer.DrawClippers(map);
			map.designationManager.DrawDesignations();
			map.overlayDrawer.DrawAllOverlays();

			renderer();

			fakeViewRect = CellRect.Empty;
			renderOffset = 0f;
			Tools.SetCurrentMapDirectly(savedMap);
			fakeZoom = false;
		}

		public static void RenderMap(State.Puppeteer puppeteer, int[] grid)
		{
			var pawn = puppeteer?.puppet?.pawn;
			pawn = Tools.GetCarrier(pawn) ?? pawn;
			if (pawn == null) return;

			var vID = puppeteer.vID;
			var px = pawn.Position.x;
			var pz = pawn.Position.z;
			var phx = pawn.DrawPos.x;
			var phz = pawn.DrawPos.z;

			if (grid == null)
			{
				var connection = Controller.instance.connection;
				if (connection != null && connection.isConnected)
					connection.Send(new GridUpdate()
					{
						controller = vID,
						info = new GridUpdate.Info()
						{
							px = px,
							pz = pz,
							phx = phx,
							phz = phz,
							map = null
						}
					});
				return;
			}

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
							map = jpgData
						}
					});
				});
			});
		}
	}
}

