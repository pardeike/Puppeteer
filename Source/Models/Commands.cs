using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using static HarmonyLib.AccessTools;

namespace Puppeteer
{
	public class SimpleCmd : JSONConvertable<SimpleCmd>
	{
	}

	public class Ping : JSONConvertable<Earned>
	{
		public bool game = true;
		public Ping() { type = "ping"; }
	}

	public class GameInfo : JSONConvertable<GameInfo>
	{
		public class Info
		{
			public string version;
		}

		public GameInfo() { type = "game-info"; }
		public ViewerID viewer;
		public Info info;
	}

	public class Earned : JSONConvertable<Earned>
	{
		public class Info
		{
			public int amount;
		}

		public Earned() { type = "earn"; }
		public ViewerID viewer;
		public Info info;
	}

	public class Portrait : JSONConvertable<Portrait>
	{
		public class Info
		{
			public byte[] image;
		}

		public Portrait() { type = "portrait"; }
		public ViewerID viewer;
		public Info info;
	}

	public class Tag
	{
		public string name;
		public string info;

		public Tag(string name, string info)
		{
			this.name = name;
			this.info = info;
		}
	}

	public class ColonistBaseInfo : JSONConvertable<ColonistBaseInfo>
	{
		public class Percentage
		{
			public string label;
			public float percent;
		}

		public class Value
		{
			public string label;
			public int r;
			public int g;
			public int b;

			public Value(string label, UnityEngine.Color color)
			{
				this.label = label;
				r = (int)(255 * color.r);
				g = (int)(255 * color.g);
				b = (int)(255 * color.b);
			}
		}

		public class NeedInfo
		{
			public string name;
			public int value;
			public int marker;
			public int[] treshholds;

			static readonly FieldRef<Need, List<float>> threshPercentsRef = FieldRefAccess<Need, List<float>>(Field(typeof(Need), "threshPercents"));

			public NeedInfo(Need rNeed)
			{
				var max = 1f;
				if (rNeed.def.scaleBar && rNeed.MaxLevel < 1f)
					max = rNeed.MaxLevel;

				name = rNeed.LabelCap;
				value = (int)(rNeed.CurLevel * 100);
				marker = rNeed.CurInstantLevelPercentage >= 0 ? (int)(rNeed.CurInstantLevelPercentage * max * 100 + 0.5f) : -1;
				treshholds = threshPercentsRef(rNeed)?.Select(p => (int)(p * 100)).ToArray() ?? Array.Empty<int>();
			}
		}

		public class ThoughtInfo
		{
			public string name;
			public int min;
			public int max;
			public int value;
		}

		public class CapacityInfo
		{
			public string name;
			public string value;
			public int[] rgb;
		}

		public class HediffInfo
		{
			public string name;
			public int count;
			public int[] rgb;
		}

		public class Injury
		{
			public string name;
			public HediffInfo[] hediffs;
			public int[] rgb;
		}

		public class SkillInfo
		{
			public string name;
			public int level;
			public int passion;
			public int[] progress;
		}

		public class Info
		{
			public string name;
			public int x;
			public int y;
			public int mx;
			public int my;
			public Tag childhood;
			public Tag adulthood;
			public string[] inspect;
			public Percentage health;
			public Percentage mood;
			public Value restrict;
			public Value area;
			public bool drafted;
			public string response;
			public NeedInfo[] needs;
			public ThoughtInfo[] thoughts;
			public CapacityInfo[] capacities;
			public int bleedingRate;
			public int deathIn;
			public Injury[] injuries;
			public SkillInfo[] skills;
			public Tag[] incapable;
			public Tag[] traits;
		}

		public ColonistBaseInfo() { type = "colonist-basics"; }
		public ViewerID viewer;
		public Info info;
	}

	public class Assignment : JSONConvertable<Assignment>
	{
		public Assignment() { type = "assignment"; }
		public ViewerID viewer;
		public bool state;
	}

	public class Join : JSONConvertable<Join>
	{
		public ViewerID viewer;
	}

	public class Leave : JSONConvertable<Leave>
	{
		public ViewerID viewer;
	}

	public class Welcome : JSONConvertable<Welcome>
	{
		public string minVersion;
	}

	public class Assign : JSONConvertable<Assign>
	{
		public int colonistID;
		public ViewerID viewer;
	}

	public class OutgoingState<T> : JSONConvertable<OutgoingState<T>>
	{
		public OutgoingState() { type = "state"; }
		public ViewerID viewer;
		public string key;
		public T val;
	}

	public class IncomingState : JSONConvertable<IncomingState>
	{
		public ViewerID user;
		public string key;
		public object val;
	}

	public class StallingState : JSONConvertable<StallingState>
	{
		public ViewerID viewer;
		public bool state;
	}

	public class ColonistInfo : JSONConvertable<ColonistInfo>
	{
		public int id;
		public string name;
		public ViewerID controller;
	}

	public class AllColonists : JSONConvertable<AllColonists>
	{
		public AllColonists() { type = "colonists"; }
		public List<ColonistInfo> colonists;
	}

	public class IncomingJob : JSONConvertable<IncomingJob>
	{
		public ViewerID user;
		public string id;
		public string method;
		public string[] args;
	}

	public class OutgoingJobResult : JSONConvertable<OutgoingJobResult>
	{
		public OutgoingJobResult() { type = "job"; }
		public ViewerID viewer;
		public string id;
		public string info; // json
	}

	public class PrioritiyInfo
	{
		public class Priorities
		{
			public string pawn;
			public bool yours;
			public int[] val; // valus from 00-24 [Passion(0-2)][Priority(0-4)] and -1 for disabled
		}

		public string[] columns;
		public bool manual;
		public int norm;
		public int max;
		public Priorities[] rows;
	}

	public class ScheduleInfo
	{
		public class Schedules
		{
			public string pawn;
			public bool yours;
			public string val;
		}

		public Schedules[] rows;
	}

	public class GridUpdate : JSONConvertable<GridUpdate>
	{
		public class Info
		{
			public int px;
			public int pz;
			public float phx;
			public float phz;
			public byte[] map;
		}

		public GridUpdate() { type = "grid"; }
		public ViewerID controller;
		public Info info;
	}

	public class ContextMenu : JSONConvertable<ContextMenu>
	{
		public class Choice
		{
			public string id;
			public string label;
			public bool disabled;
		}

		public ContextMenu() { type = "menu"; }
		public ViewerID controller;
		public Choice[] choices;
	}
}