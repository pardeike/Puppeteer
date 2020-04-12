using HarmonyLib;
using Verse;

// TODOs:
// - find out if LabelCap is better using Resolve() or ToString()
// - add settings

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