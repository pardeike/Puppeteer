using RimWorld;
using UnityEngine;
using Verse;
using static Harmony.AccessTools;

namespace Puppeteer
{
	public static class Renderer
	{
		const int imageSize = 256;
		public static float renderOffset = 0f;
		public static Vector3 RenderOffsetVector => new Vector3(renderOffset, 0f, 0f);
		public static CellRect fakeViewRect = CellRect.Empty;
		public static bool fakeZoom = false;
		static readonly FieldRef<SubcameraDriver, Camera[]> subcamerasRef = FieldRefAccess<SubcameraDriver, Camera[]>("subcameras");

		public static byte[] GetPawnPortrait(Pawn pawn, int size)
		{
			var renderTexture = PortraitsCache.Get(pawn, new Vector2(size, size), new Vector3(0f, 0f, 0.1f), 1.28f);
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

		public static void PawnScreenRender(Pawn pawn, float radius)
		{
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
			var imageTexture = new Texture2D(imageSize, imageSize, TextureFormat.RGB24, false);
			imageTexture.ReadPixels(new Rect(0, 0, imageSize, imageSize), 0, 0, false);
			imageTexture.Apply();
			RenderTexture.ReleaseTemporary(renderTexture);
			camera.targetTexture = null;
			RenderTexture.active = null;

			SetCamera(camera, ref rememberPosition, rememberOrthographicSize);
			camera.farClipPlane = rememberFarClipPlane;

			var jpgData = imageTexture.EncodeToJPG(50);
			Puppeteer.instance.PawnOnMap(pawn, jpgData);
		}
	}
}
