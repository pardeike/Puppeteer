using RimWorld.Planet;
using Verse;

namespace Puppeteer
{
	public class Tickets : WorldComponent
	{
		public int remaining;

		public Tickets(World world) : base(world)
		{
			remaining = PuppeteerMod.Settings.startTickets;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref remaining, "remaining");
		}
	}
}