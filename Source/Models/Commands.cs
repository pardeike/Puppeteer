using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using static HarmonyLib.AccessTools;

namespace Puppeteer
{
	public class SimpleCmd : JSONConvertable<SimpleCmd>
	{
		public string type;
	}

	public class Ping : JSONConvertable<Earned>
	{
		public string type = "ping";
		public bool game = true;
	}

	public class Earned : JSONConvertable<Earned>
	{
		public class Info
		{
			public int amount;
		}

		public string type = "earn";
		public ViewerID viewer;
		public Info info;
	}

	public class Portrait : JSONConvertable<Portrait>
	{
		public class Info
		{
			public byte[] image;
		}

		public string type = "portrait";
		public ViewerID viewer;
		public Info info;
	}

	public class OnMap : JSONConvertable<OnMap>
	{
		public class Info
		{
			public byte[] image;
		}

		public string type = "on-map";
		public ViewerID viewer;
		public Info info;
	}

	public class ColonistBaseInfo : JSONConvertable<OnMap>
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
		}

		public string type = "colonist-basics";
		public ViewerID viewer;
		public Info info;
	}

	public class Assignment : JSONConvertable<Assignment>
	{
		public string type = "assignment";
		public ViewerID viewer;
		public bool state;
	}

	public class Join : JSONConvertable<Join>
	{
		public string type;
		public ViewerID viewer;
	}

	public class Leave : JSONConvertable<Leave>
	{
		public string type;
		public ViewerID viewer;
	}

	public class Assign : JSONConvertable<Assign>
	{
		public string type;
		public int colonistID;
		public ViewerID viewer;
	}

	public class OutgoingState<T> : JSONConvertable<OutgoingState<T>>
	{
		public string type = "state";
		public ViewerID viewer;
		public string key;
		public T val;
	}

	public class IncomingState : JSONConvertable<IncomingState>
	{
		public string type;
		public ViewerID user;
		public string key;
		public object val;
	}

	public class ColonistInfo : JSONConvertable<ColonistInfo>
	{
		public int id;
		public string name;
		public ViewerID controller;
		public string lastSeen;
	}

	public class AllColonists : JSONConvertable<AllColonists>
	{
		public string type = "colonists";
		public List<ColonistInfo> colonists;
	}

	public class IncomingJob : JSONConvertable<IncomingJob>
	{
		public string type;
		public ViewerID user;
		public string id;
		public string method;
		public string[] args;
	}

	public class OutgoingJobResult : JSONConvertable<OutgoingJobResult>
	{
		public string type = "job";
		public ViewerID viewer;
		public string id;
		public string info; // json
	}
}