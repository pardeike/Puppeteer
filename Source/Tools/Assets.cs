using UnityEngine;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class Assets
	{
		public static Texture2D puppet = ContentFinder<Texture2D>.Get("Puppet", true);
		public static Texture2D bubble = ContentFinder<Texture2D>.Get("Bubble", true);
		public static Texture2D[] connectedMin = new[]
		{
			ContentFinder<Texture2D>.Get("Connected-Min-0", true),
			ContentFinder<Texture2D>.Get("Connected-Min-1", true),
		};
		public static Texture2D[] connectedMax = new[]
		{
			ContentFinder<Texture2D>.Get("Connected-Max-0", true),
			ContentFinder<Texture2D>.Get("Connected-Max-1", true)
		};
	}
}