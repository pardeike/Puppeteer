using Verse;
using static HarmonyLib.AccessTools;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class GetterSetters
	{
		// Dialogs
		public static FieldRef<Listing, float> curXByRef = FieldRefAccess<Listing, float>("curX");
		public static FieldRef<Listing, float> curYByRef = FieldRefAccess<Listing, float>("curY");

		static GetterSetters()
		{
		}
	}
}