using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Puppeteer
{
	public class State
	{
		const string saveFileName = "PuppeteerState.json";

		public static State instance = Load();

		public class Puppet
		{
			public Pawn pawn;
			public Puppeteer puppeteer; // optional
		}

		public class Puppeteer
		{
			public ViewerID vID;
			public Puppet puppet; // optional
			public bool connected;
			public DateTime lastCommandIssued;
			public string lastCommand;
			public int coinsEarned;
			public int gridSize;
		}

		// new associations are automatically create for:
		//
		//  Viewer  ---creates--->  Puppeteer  ---optionally-has--->  Puppet
		//  Pawn    ---creates--->  Puppet     ---optionally-has--->  Puppeteer
		//
		public Dictionary<Pawn, Puppet> pawnToPuppet = new Dictionary<Pawn, Puppet>(); // int == pawn.thingID
		public Dictionary<ViewerID, Puppeteer> viewerToPuppeteer = new Dictionary<ViewerID, Puppeteer>();

		//

		static State Load()
		{
			var data = saveFileName.ReadConfig();
			if (data == null) return new State();
			return JsonConvert.DeserializeObject<State>(data);
		}

		public void Save()
		{
			var data = JsonConvert.SerializeObject(this);
			PuppetCommentator.Say($"Saving {data.Length} bytes");
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
				vID = vID,
				lastCommandIssued = DateTime.Now,
				lastCommand = "Became a puppeteer"
			};
			viewerToPuppeteer.Add(vID, puppeteer);
			return puppeteer;
		}

		public IEnumerable<ViewerID> AllViewers()
		{
			return viewerToPuppeteer.Keys;
		}

		public IEnumerable<ViewerID> ConnectedViewers()
		{
			return viewerToPuppeteer
				.Where(pair => pair.Value.connected)
				.Select(pair => pair.Key);
		}

		public IEnumerable<ViewerID> AvailableViewers()
		{
			return viewerToPuppeteer
				.Where(pair => pair.Value.puppet == null)
				.Select(pair => pair.Key);
		}

		public IEnumerable<Puppeteer> ConnectedPuppeteers()
		{
			return viewerToPuppeteer.Values
				.Where(puppeteer => puppeteer.connected);
		}

		public bool HasPuppet(ViewerID vID)
		{
			return PuppeteerForViewer(vID)?.puppet != null;
		}

		public void Assign(ViewerID vID, Pawn pawn)
		{
			if (pawn == null) return;
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
			var pawn = puppeteer.puppet?.pawn;
			if (pawn != null)
				Tools.SetColonistNickname(pawn, connected ? vID.name : null);
		}

		// pawns

		public Puppet PuppetForPawn(Pawn pawn)
		{
			if (pawn == null) return null;
			_ = pawnToPuppet.TryGetValue(pawn, out var puppet);
			return puppet;
		}

		public void AddPawn(Pawn pawn)
		{
			if (pawn == null) return;
			pawnToPuppet.Add(pawn, new Puppet()
			{
				pawn = pawn,
				puppeteer = null
			});
		}

		public void RemovePawn(Pawn pawn)
		{
			if (pawn == null) return;
			if (pawnToPuppet.TryGetValue(pawn, out var puppet) && puppet.puppeteer != null)
				puppet.puppeteer.puppet = null;
			_ = pawnToPuppet.Remove(pawn);
		}

		public bool? IsConnected(Pawn pawn)
		{
			if (pawn == null) return null;
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