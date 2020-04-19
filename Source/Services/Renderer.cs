using RimWorld;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class Renderer
	{
		const int imageSize = 128;
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
			return portrait.EncodeToPNG();
			//var compressor = new TJCompressor();
			//var ptr = portrait.GetNativeTexturePtr();
			//var stride = size * 4;
			//var data = compressor.Compress(ptr, stride, size, size, TJPixelFormats.TJPF_ARGB, TJSubsamplingOptions.TJSAMP_444, 75, TJFlags.NONE);
		}

		public static void SetCamera(Camera camera, ref Vector3 position, float size)
		{
			camera.orthographicSize = size;
			camera.farClipPlane = 100f;
			camera.transform.position = position;
		}

		public static void PawnScreenRender(ViewerID vID, Vector3 drawPos, float radius)
		{
			var camera = RenderCamera.camera;
			if (camera == null) return;

			var cameraPos = new Vector3(renderOffset + drawPos.x, 40f, drawPos.z);
			SetCamera(camera, ref cameraPos, radius);

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

			var jpgData = imageTexture.EncodeToJPG(60);
			PuppeteerController.instance.PawnOnMap(vID, jpgData);
		}
	}
}