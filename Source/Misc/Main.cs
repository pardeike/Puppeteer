using Harmony;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	class Main
	{
		static Main()
		{
			var harmony = HarmonyInstance.Create("net.pardeike.harmony.Puppeteer");
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