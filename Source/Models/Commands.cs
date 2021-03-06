﻿using Newtonsoft.Json;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Puppeteer
{
	public class SimpleCmd : JSONConvertable<SimpleCmd>
	{
	}

	public class Ping : JSONConvertable<Ping>
	{
		public bool game = true;
		public Ping() { type = "ping"; }
	}

	public class GameInfo : JSONConvertable<GameInfo>
	{
		public class ColonistStyle
		{
			public string gender;
			public string hairStyle;
			public string bodyType;
			public int melanin;
			public int[] hairColor;
		}

		public class Info
		{
			public string version;
			public int mapFreq;
			public string[] hairStyles;
			public string[] bodyTypes;
			public string[] features;
			public ColonistStyle style;
		}

		public GameInfo() { type = "game-info"; }
		public ViewerID viewer;
		public Info info;
	}

	public class TimeInfo : JSONConvertable<TimeInfo>
	{
		public class Info
		{
			public string time;
			public int speed;
		}

		public TimeInfo() { type = "time-info"; }
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

	public class OutgoingChat : JSONConvertable<OutgoingChat>
	{
		public OutgoingChat() { type = "chat"; }
		public ViewerID viewer;
		public string message;
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

			public NeedInfo(Need rNeed)
			{
				var max = 1f;
				if (rNeed.def.scaleBar && rNeed.MaxLevel < 1f)
					max = rNeed.MaxLevel;

				name = rNeed.LabelCap;
				value = (int)(rNeed.CurLevel * 100);
				marker = rNeed.CurInstantLevelPercentage >= 0 ? (int)(rNeed.CurInstantLevelPercentage * max * 100 + 0.5f) : -1;
				treshholds = rNeed.threshPercents?.Select(p => (int)(p * 100)).ToArray() ?? Array.Empty<int>();
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

	public class ColonistAvailable : JSONConvertable<ColonistAvailable>
	{
		public ColonistAvailable() { type = "colonist-available"; }
		public ViewerID viewer;
		public bool state;
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

	public class IncomingChat : JSONConvertable<IncomingChat>
	{
		public string message;
		public ViewerID viewer;
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

	public class SocialRelations : JSONConvertable<SocialRelations>
	{
		public class Opinion
		{
			public string reason;
			public string value;
		}

		public class Relation
		{
			[JsonIgnore] public List<PawnRelationDef> _relations;
			[JsonIgnore] public int _ourOpinionNum;

			public string type;
			public string pawn;
			public byte[] portrait;
			public Opinion[] opinions;
			public string ourOpinion;
			public string theirOpinion;
			public string situation;
		}

		public class Info
		{
			public Relation[] relations;
			public string lastInteraction;
		}

		public SocialRelations() { type = "socials"; }
		public ViewerID viewer;
		public Info info;
	}

	public class Gear : JSONConvertable<Gear>
	{
		public class Apparel
		{
			public string id;
			public string name;
			public int quality;
			public bool tainted;
			public bool forced;
			public int hp1;
			public int hp2;
			public float mValue;
			public string stuff;
			public string mass;
			public string aSharp;
			public string aBlunt;
			public string aHeat;
			public string iCold;
			public string iHeat;
			public byte[] preview;
		}

		public class BodyPart
		{
			public string name;
			public Apparel[] apparels;
		}

		public class Info
		{
			public string currentMass;
			public string maxMass;
			public string[] comfortableTemps;
			public string[] overallArmor;
			public BodyPart[] parts;
		}

		public Gear() { type = "gear"; }
		public ViewerID viewer;
		public Info info;
	}

	public class Inventory : JSONConvertable<Inventory>
	{
		public class Item
		{
			public string id;
			public string name;
			public string mass;
			public byte[] preview;
			public bool consumable;
		}

		public class Info
		{
			public Item[] inventory;
			public Item[] equipment;
		}

		public Inventory() { type = "inventory"; }
		public ViewerID viewer;
		public Info info;
	}

	public class GridUpdate : JSONConvertable<GridUpdate>
	{
		public class Frame
		{
			public int x1;
			public int z1;
			public int x2;
			public int z2;

			public Frame(int[] n)
			{
				x1 = n[0];
				z1 = n[1];
				x2 = n[2];
				z2 = n[3];
			}
		}

		public class Info
		{
			public int px;
			public int pz;
			public float phx;
			public float phz;
			public Frame frame;
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

	public class Selection : JSONConvertable<Selection>
	{
		public class Gizmo
		{
			public string id;
			public string label;
			public string disabled;
			public bool allowed;
		}

		public class Corner
		{
			public int x;
			public int z;

			public Corner(Vector3 vec)
			{
				x = (int)vec.x;
				z = (int)vec.z;
			}
		}

		public Selection() { type = "selection"; }
		public ViewerID controller;
		public Corner[] frame;
		public Gizmo[] gizmos;
		public byte[] atlas;
	}

	public class Customize : JSONConvertable<Customize>
	{
		public string key;
		public string val;
		public ViewerID viewer;
	}

	public class ToolkitCommands : JSONConvertable<ToolkitCommands>
	{
		public ToolkitCommands() { type = "toolkit-commands"; }
		public string[] commands;
		public ViewerID viewer;
	}
}
