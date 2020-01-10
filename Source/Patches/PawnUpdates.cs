using Harmony;
using Verse;

namespace Puppeteer
{
	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.Position), MethodType.Setter)]
	static class Thing_Position_Patch
	{
		public static void Postfix(Thing __instance)
		{
			var pawn = __instance as Pawn;
			if (pawn == null || pawn.Spawned == false || pawn.IsColonist == false)
				return;

			Puppeteer.instance.PawnUpdate(pawn);
		}
	}
}