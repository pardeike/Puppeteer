using System;
using UnityEngine;
using Verse;

namespace Puppeteer.Core
{
	public static class ColonistCameraManager
	{
		static ColonistCameraManager()
		{
			Camera = CreateCamera();
		}

		public static Camera Camera { get; private set; }

		public static bool Active
		{
			get => Camera.gameObject.activeInHierarchy;
			set => Camera.gameObject.SetActive(value);
		}

		public static ColonistCameraDriver GetDriver()
		{
			return Camera.GetComponent<ColonistCameraDriver>();
		}

		static Camera CreateCamera()
		{
			var standardCamera = Find.Camera;

			var gameObject = new GameObject("ColonistCamera", new Type[] {
				typeof(Camera)
			});
			gameObject.SetActive(false);
			_ = gameObject.AddComponent<ColonistCameraDriver>();
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			var cam = gameObject.GetComponent<Camera>();
			cam.depth = -10;
			//cam.forceIntoRenderTexture = true;
			//cam.transform.position = Vector3.zero;
			cam.transform.rotation = standardCamera.transform.rotation;
			//cam.fieldOfView = standardCamera.fieldOfView;
			//cam.nearClipPlane = standardCamera.nearClipPlane;
			//cam.farClipPlane = standardCamera.farClipPlane;
			//cam.renderingPath = standardCamera.renderingPath;
			//cam.allowHDR = standardCamera.allowHDR;
			//cam.allowMSAA = standardCamera.allowMSAA;
			//cam.orthographicSize = standardCamera.orthographicSize;
			cam.orthographic = true;
			//cam.opaqueSortMode = standardCamera.opaqueSortMode;
			//cam.transparencySortMode = standardCamera.transparencySortMode;
			//cam.transparencySortAxis = standardCamera.transparencySortAxis;
			//cam.aspect = standardCamera.aspect;
			//cam.cullingMask = standardCamera.cullingMask;
			//cam.eventMask = standardCamera.eventMask;
			//cam.backgroundColor = standardCamera.backgroundColor;
			//cam.clearFlags = standardCamera.clearFlags;
			//cam.useOcclusionCulling = standardCamera.useOcclusionCulling;
			//cam.layerCullDistances = standardCamera.layerCullDistances;
			//cam.layerCullSpherical = standardCamera.layerCullSpherical;
			//cam.depthTextureMode = standardCamera.depthTextureMode;
			return cam;
		}
	}

	public class ColonistCameraDriver : MonoBehaviour
	{
		private Camera cachedCamera;

		public Camera MyCamera
		{
			get
			{
				if (cachedCamera == null) cachedCamera = GetComponent<Camera>();
				return cachedCamera;
			}
		}

		public void OnPostRender()
		{
		}

		/*public void UpdateTexture(int width, int height)
		{
			var renderTexture = camera.targetTexture;
			if (renderTexture != null && (renderTexture.width != width || renderTexture.height != height))
			{
				Destroy(renderTexture);
				renderTexture = null;
			}
			if (renderTexture == null)
				renderTexture = new RenderTexture(width, height, 24);
			if (!renderTexture.IsCreated())
				_ = renderTexture.Create();
			camera.targetTexture = renderTexture;
		}*/
	}
}