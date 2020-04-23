using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class Assets
	{
		public static Texture2D puppet = LoadTexture("Puppet");
		public static Texture2D bubble = LoadTexture("Bubble");
		public static Texture2D[] connected = LoadTextures("Connected0", "Connected1");

		static Texture2D LoadTexture(string path)
		{
			var fullPath = Path.Combine(Tools.GetModRootDirectory(), "Textures", $"{path}.png");
			var data = File.ReadAllBytes(fullPath);
			if (data == null || data.Length == 0) throw new Exception($"Cannot read texture {fullPath}");
			var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
			if (tex.LoadImage(data) == false) throw new Exception($"Cannot create texture {fullPath}");
			tex.Compress(true);
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.filterMode = FilterMode.Trilinear;
			tex.Apply(true, true);
			return tex;
		}

		static Texture2D[] LoadTextures(params string[] paths)
		{
			// ContentFinder<Texture2D>.Get(path, true)
			return paths.Select(path => LoadTexture(path)).ToArray();
		}
	}
}