using Harmony;
using JsonFx.Json;
using System.Diagnostics;
using System.Text;
using Verse;

namespace Puppeteer
{
	class PuppeteerMain : Mod
	{
		public PuppeteerMain(ModContentPack content) : base(content)
		{
			var harmony = HarmonyInstance.Create("net.pardeike.harmony.Puppeteer");
			harmony.PatchAll();

			_ = Connection.Instance;
		}
	}

	class Patches
	{
		[HarmonyPatch(typeof(Thing))]
		[HarmonyPatch(nameof(Thing.Position), MethodType.Setter)]
		static class Thing_Position_Patch
		{
			static long secs = 0;
			static long min = 1000000;
			static long max = -100000;
			static long n = 0;
			static bool failure = false;
			static int counter = 0;

			static void Postfix(Thing __instance)
			{
				var pawn = __instance as Pawn;
				if (pawn == null || pawn.Spawned == false || pawn.IsColonist == false)
					return;

				var colonist = new colonist(pawn);

				var sb = new StringBuilder();
				using (var writer = new JsonWriter(sb))
				{
					writer.Write(colonist);
				}
				var stopWatch = new Stopwatch();
				stopWatch.Start();
				Connection.Instance.ws.SendData(sb.ToString(), 10, (success) =>
				{
					if (success == false) failure = true;
					var d = stopWatch.ElapsedMilliseconds;
					if (d < min) min = d;
					if (d > max) max = d;
					secs += d;
					n++;
					stopWatch.Stop();
				});

				if (++counter >= 60)
				{
					Log.Warning(((float)secs / n) + " " + min + "-" + max + " " + (failure ? "F" : ""));
					secs = 0;
					n = 0;
					min = 1000000;
					max = -100000;
					counter = 0;
					failure = false;
				}
			}
		}
	}
}