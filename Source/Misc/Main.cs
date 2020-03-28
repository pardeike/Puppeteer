using HarmonyLib;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	class Main
	{
		static Main()
		{
			var harmony = new Harmony("net.pardeike.harmony.Puppeteer");
			harmony.PatchAll();
		}
	}

	public class PuppeteerMain : Mod
	{
		public PuppeteerMain(ModContentPack content) : base(content)
		{
		}
	}
}