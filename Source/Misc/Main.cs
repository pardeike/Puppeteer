using Harmony;
using Verse;

namespace Puppeteer
{
	class PuppeteerMain : Mod
	{
		public static Puppeteer puppeteer;

		public PuppeteerMain(ModContentPack content) : base(content)
		{
			puppeteer = new Puppeteer();

			var harmony = HarmonyInstance.Create("net.pardeike.harmony.Puppeteer");
			harmony.PatchAll();
		}
	}
}