using System;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class RenderCamera
	{
		public static Camera camera = null;

		public static void Create()
		{
			var originalCamera = Find.Camera;
			var gameObject = new GameObject("PuppeteerRenderCamera", new Type[] { typeof(Camera) })
			{
				transform =
				{
					parent = originalCamera.transform,
					localPosition = Vector3.zero,
					localScale = Vector3.one,
					localRotation = Quaternion.identity
				}
			};
			gameObject.SetActive(false);
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			camera = gameObject.GetComponent<Camera>();
			camera.orthographic = originalCamera.orthographic;
			camera.orthographicSize = originalCamera.orthographicSize;
			camera.nearClipPlane = originalCamera.nearClipPlane;
			camera.farClipPlane = originalCamera.farClipPlane;
			camera.useOcclusionCulling = originalCamera.useOcclusionCulling;
			camera.allowHDR = originalCamera.allowHDR;
			camera.renderingPath = originalCamera.renderingPath;
			camera.clearFlags = CameraClearFlags.Color;
			camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
			camera.depth = originalCamera.depth;
		}
	}
}