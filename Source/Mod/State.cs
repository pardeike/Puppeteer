using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Puppeteer
{
	public class State
	{
		const string saveFileName = "PuppeteerState.json";

		static State _instance;
		public static State Instance
		{
			get
			{
				if (_instance == null) _instance = Load();
				return _instance;
			}
		}

		public class Puppet
		{
			[JsonProperty] internal int _id;
			internal void Init(ref int id) { _id = ++id; }
			internal void Update()
			{
				_pawn = pawn?.thingIDNumber ?? 0;
				_puppeteer = puppeteer?._id ?? 0;
			}
			internal void Restore(State state)
			{
				pawn = Tools.ColonistForThingID(_pawn);
				puppeteer = state.viewerToPuppeteer.Values.FirstOrDefault(v => v != null && v._id == _puppeteer);
			}

			[JsonIgnore] public Pawn pawn;
			[JsonProperty] private int _pawn;

			[JsonIgnore] public Puppeteer puppeteer; // optional
			[JsonProperty] private int _puppeteer;

			public int lastPlayerCommand;

			// ========= CMD --[cooldown-length]--> END =======>
			// ==========> 1
			// ====================> 0.5
			// =====================================> 0
			// ===========================================> -x
			//
			public float CooldownFactor()
			{
				var now = Find.TickManager.TicksAbs;
				var cooldownTicks = PuppeteerMod.Settings.playerActionCooldownTicks;
				return GenMath.LerpDoubleClamped(lastPlayerCommand, lastPlayerCommand + cooldownTicks, 1, 0, Find.TickManager.TicksAbs);
			}
		}

		public class Puppeteer
		{
			[JsonProperty] internal int _id;
			internal void Init(ref int id) { _id = ++id; }
			internal void Update()
			{
				_puppet = puppet?._id ?? 0;
			}
			internal void Restore(State state)
			{
				puppet = state.pawnToPuppet.Values.FirstOrDefault(v => v._id == _puppet);
				lastCommand = null;
				lastCommandIssued = default;
				connected = default;
				stalling = default;
				lastCommandIssued = default;
				lastCommand = default;
			}

			public ViewerID vID;

			[JsonIgnore] public Puppet puppet; // optional
			[JsonProperty] private int _puppet;

			public bool connected;
			public bool stalling;
			public DateTime lastCommandIssued;
			public string lastCommand;

			public bool IsConnected => connected && stalling == false;
		}

		// new associations are automatically create for:
		//
		//  Viewer  ---creates--->  Puppeteer  ---optionally-has--->  Puppet
		//  Pawn    ---creates--->  Puppet     ---optionally-has--->  Puppeteer
		//
		public ConcurrentDictionary<int, Puppet> pawnToPuppet = new ConcurrentDictionary<int, Puppet>(); // int == pawn.thingID
		public ConcurrentDictionary<string, Puppeteer> viewerToPuppeteer = new ConcurrentDictionary<string, Puppeteer>();

		//

		public static HashSet<Pawn> pawnsToRefresh = new HashSet<Pawn>();

		//

		static State Load()
		{
			var data = saveFileName.ReadConfig();
			if (data == null) return new State();
			var state = JsonConvert.DeserializeObject<State>(data);
			state.viewerToPuppeteer.Values.Do(p => p?.Restore(state));
			state.pawnToPuppet.Values.Do(p => p.Restore(state));
			return state;
		}

		public static void Save()
		{
			if (_instance == null) return;
			var id = 0;
			_instance.viewerToPuppeteer.Values.Do(p => p.Init(ref id));
			_instance.pawnToPuppet.Values.Do(p => p?.Init(ref id));
			_instance.viewerToPuppeteer.Values.Do(p => p.Update());
			_instance.pawnToPuppet.Values.Do(p => p?.Update());
			var data = JsonConvert.SerializeObject(_instance, Tools.IsLocalDev ? Formatting.Indented : Formatting.None);
			saveFileName.WriteConfig(data);
		}

		// viewers

		public Puppeteer PuppeteerForViewerName(string name)
		{
			return viewerToPuppeteer.Values.FirstOrDefault(puppeteer => puppeteer.vID.name.ToLower() == name.ToLower());
		}

		public Puppeteer PuppeteerForViewer(ViewerID vID)
		{
			if (vID == null) return null;
			_ = viewerToPuppeteer.TryGetValue(vID.Identifier, out var puppeteer);
			return puppeteer;
		}

		Puppeteer CreatePuppeteerForViewer(ViewerID vID)
		{
			if (vID == null) return null;
			var puppeteer = new Puppeteer()
			{
				vID = vID,
				lastCommandIssued = DateTime.Now,
				lastCommand = "became-puppeteer"
			};
			_ = viewerToPuppeteer.TryAdd(vID.Identifier, puppeteer);
			return puppeteer;
		}

		/*public IEnumerable<ViewerID> AllViewers()
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
		}*/

		public List<Puppeteer> AllPuppeteers()
		{
			return viewerToPuppeteer.Values.Where(puppeteer => puppeteer != null).ToList();
		}

		public IEnumerable<Puppeteer> ConnectedPuppeteers()
		{
			return viewerToPuppeteer.Values
				.Where(puppeteer => puppeteer != null && puppeteer.IsConnected);
		}

		public void Assign(ViewerID vID, Pawn pawn)
		{
			if (vID == null || pawn == null) return;
			var puppet = PuppetForPawn(pawn);
			if (puppet == null) return;
			var puppeteer = PuppeteerForViewer(vID);
			if (puppeteer == null) return;
			puppeteer.puppet = puppet;
			puppet.puppeteer = puppeteer;
		}

		public void Unassign(ViewerID vID)
		{
			if (vID == null) return;
			var puppeteer = PuppeteerForViewer(vID);
			if (puppeteer == null) return;
			if (puppeteer.puppet != null)
				puppeteer.puppet.puppeteer = null;
			puppeteer.puppet = null;
		}

		public void SetConnected(ViewerID vID, bool connected)
		{
			if (vID == null) return;
			var puppeteer = PuppeteerForViewer(vID) ?? CreatePuppeteerForViewer(vID);
			puppeteer.connected = connected;
			puppeteer.lastCommandIssued = DateTime.Now;
			//var pawn = puppeteer.puppet?.pawn;
			//if (pawn != null)
			//	Tools.SetColonistNickname(pawn, connected ? vID.name : null);
		}

		// pawns

		public Puppet PuppetForPawn(Pawn pawn)
		{
			if (pawn == null || pawn.Spawned == false) return null;
			_ = pawnToPuppet.TryGetValue(pawn.thingIDNumber, out var puppet);
			return puppet;
		}

		public void UpdatePawn(Pawn pawn)
		{
			if (pawn == null || pawn.Spawned == false) return;
			var puppet = PuppetForPawn(pawn);
			if (puppet != null)
			{
				puppet.pawn = pawn;
				return;
			}
			_ = pawnToPuppet.TryAdd(pawn.thingIDNumber, new Puppet()
			{
				pawn = pawn,
				puppeteer = null
			});
		}

		public bool RemovePawn(Pawn pawn)
		{
			if (pawn == null || pawn.Spawned == false) return false;
			if (pawnToPuppet.TryRemove(pawn.thingIDNumber, out var puppet))
			{
				if (puppet?.puppeteer != null)
					puppet.puppeteer.puppet = null;
				return true;
			}
			return false;
		}

		public void ResetLastControlled()
		{
			AllPuppets().Do(puppet => puppet.lastPlayerCommand = 0);
		}

		public HashSet<Puppet> AllPuppets()
		{
			return pawnToPuppet.Values.ToHashSet();
		}

		public HashSet<Puppet> AssignedPuppets()
		{
			return viewerToPuppeteer.Values.Where(puppeteer => puppeteer != null).Select(puppeteer => puppeteer.puppet).OfType<Puppet>().ToHashSet();
		}

		public IEnumerable<Puppet> AvailablePuppets()
		{
			return AllPuppets().Except(AssignedPuppets());
		}
	}
}