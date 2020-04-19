using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Verse;

namespace Puppeteer
{
	[TypeConverter(typeof(PawnConverter))]
	public class SPawn : Pawn { }

	[TypeConverter(typeof(PawnConverter))]
	public class PawnConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof(string)) return true;
			return base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			var thingID = Tools.SafeParse(value as string);
			if (thingID.HasValue)
				return Tools.ColonistForThingID(thingID.Value);
			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			var pawn = value as Pawn;
			if (pawn != null && destinationType == typeof(string)) { return pawn.ThingID; }
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	public class State
	{
		const string saveFileName = "PuppeteerState.json";

		public class Puppet
		{
			public Pawn pawn;
			public Puppeteer puppeteer; // optional
		}

		public class Puppeteer
		{
			public Puppet puppet; // optional
			public bool connected;
			public DateTime lastCommandIssued;
			public string lastCommand;
			public int coinsEarned;
		}

		// new associations are automatically create for:
		//
		//  Viewer  ---creates--->  Puppeteer  ---optionally-has--->  Puppet
		//  Pawn    ---creates--->  Puppet     ---optionally-has--->  Puppeteer
		//
		readonly Dictionary<SPawn, Puppet> pawnToPuppet = new Dictionary<SPawn, Puppet>();
		readonly Dictionary<ViewerID, Puppeteer> viewerToPuppeteer = new Dictionary<ViewerID, Puppeteer>();

		//

		public static State Load()
		{
			var data = saveFileName.ReadConfig();
			if (data == null) return new State();
			return JsonConvert.DeserializeObject<State>(data);
		}

		public void Save()
		{
			var data = JsonConvert.SerializeObject(this);
			saveFileName.WriteConfig(data);
		}

		// viewers

		public Puppeteer PuppeteerForViewer(ViewerID vID)
		{
			_ = viewerToPuppeteer.TryGetValue(vID, out var puppeteer);
			return puppeteer;
		}

		Puppeteer CreatePuppeteerForViewer(ViewerID vID)
		{
			var puppeteer = new Puppeteer()
			{
				puppet = null,
				connected = false,
				lastCommandIssued = DateTime.Now,
				lastCommand = "Became a puppeteer",
				coinsEarned = 0
			};
			viewerToPuppeteer.Add(vID, puppeteer);
			return puppeteer;
		}

		public IEnumerable<ViewerID> AvailableViewers()
		{
			return viewerToPuppeteer
				.Where(pair => pair.Value.puppet == null)
				.Select(pair => pair.Key);
		}

		public bool HasPuppet(ViewerID vID)
		{
			return PuppeteerForViewer(vID)?.puppet != null;
		}

		public void Assign(ViewerID vID, SPawn pawn)
		{
			var puppet = PuppetForPawn(pawn);
			if (puppet == null) return;
			var puppeteer = PuppeteerForViewer(vID);
			if (puppeteer == null) return;
			puppeteer.puppet = puppet;
			puppet.puppeteer = puppeteer;
		}

		public void Unassign(ViewerID vID)
		{
			var puppeteer = PuppeteerForViewer(vID);
			if (puppeteer == null) return;
			if (puppeteer.puppet != null)
				puppeteer.puppet.puppeteer = null;
			puppeteer.puppet = null;
		}

		public void SetConnected(ViewerID vID, bool connected)
		{
			var puppeteer = PuppeteerForViewer(vID) ?? CreatePuppeteerForViewer(vID);
			puppeteer.connected = connected;
		}

		// pawns

		public Puppet PuppetForPawn(SPawn pawn)
		{
			_ = pawnToPuppet.TryGetValue(pawn, out var puppet);
			return puppet;
		}

		public void AddPawn(SPawn pawn)
		{
			pawnToPuppet[pawn] = new Puppet()
			{
				pawn = pawn,
				puppeteer = null
			};
		}

		public bool? IsConnected(SPawn pawn)
		{
			var puppet = PuppetForPawn(pawn);
			var puppeteer = puppet?.puppeteer;
			if (puppeteer == null) return null;
			return puppeteer.connected;
		}

		public HashSet<Puppet> AllPuppets()
		{
			return pawnToPuppet.Values.ToHashSet();
		}

		public HashSet<Puppet> AssignedPuppets()
		{
			return viewerToPuppeteer.Values.Select(puppeteer => puppeteer.puppet).OfType<Puppet>().ToHashSet();
		}

		public IEnumerable<Puppet> AvailablePuppets()
		{
			return AllPuppets().Except(AssignedPuppets());
		}
	}
}