using System.Linq;
using Verse;

namespace Puppeteer
{
	public class ResurrectionPortal : Building
	{
		public int created;

		public ResurrectionPortal() : base()
		{
			created = Find.TickManager.TicksGame;
		}

		public static ResurrectionPortal PortalForMap(Map map)
		{
			return map.listerThings.AllThings.OfType<ResurrectionPortal>().FirstOrDefault();
		}

		public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = true;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref created, "created");
		}

		public override string GetInspectString()
		{
			var tickets = Find.World.GetComponent<Tickets>();
			return $"You have {tickets.remaining} spawn tickets left.";
		}
	}

	public class PlaceWorker_ResurrectionPortal : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			if (ResurrectionPortal.PortalForMap(map) != null)
				return AcceptanceReport.WasRejected;
			return AcceptanceReport.WasAccepted;
		}
	}
}