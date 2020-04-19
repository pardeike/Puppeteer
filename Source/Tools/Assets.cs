using UnityEngine;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class Assets
	{
		public static Texture2D puppet = ContentFinder<Texture2D>.Get("Puppet", true);
		public static Texture2D bubble = ContentFinder<Texture2D>.Get("Bubble", true);
		public static Texture2D[] connected = new[]
		{
			ContentFinder<Texture2D>.Get("Connected0", true),
			ContentFinder<Texture2D>.Get("Connected1", true)
		};
	}
}