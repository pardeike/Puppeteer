using JsonFx.Json;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Puppeteer
{
	public class TokenJSON
	{
		public string service;
		public string id;
		public string game;
		public int version;
		public int iat;

		public static TokenJSON Create(string str)
		{
			var reader = new JsonReader(str);
			return reader.Deserialize<TokenJSON>();
		}
	}

	public class DataJSON
	{
		public PawnJSON pawn;

		public DataJSON(Verse.Pawn p)
		{
			pawn = PawnJSON.Make(p);
		}
	}

	public class CellJSON
	{
		public int x;
		public int z;

		public static CellJSON Make(IntVec3? c)
		{
			if (c == null) return null;
			return new CellJSON()
			{
				x = c.Value.x,
				z = c.Value.z
			};
		}
	}

	public class ThingJSON
	{
		public string def;
		public int hitPoints;
		public string quality;
		public float beauty = -1;
		public float insCold = -1;
		public float insHeat = -1;
		public float mass = -1;
		public float move = -1;

		public static ThingJSON Make(Verse.Thing thing)
		{
			if (thing == null) return null;
			if (thing is Verse.Pawn) return PawnJSON.Make(thing);
			var result = new ThingJSON()
			{
				def = thing.def.defName,
				hitPoints = thing.HitPoints,
			};
			if (thing is ThingWithComps comp)
			{
				result.quality = comp.TryGetComp<CompQuality>()?.Quality.GetLabel();
				result.beauty = comp.GetStatValue(StatDefOf.Beauty);
				result.insCold = comp.GetStatValue(StatDefOf.Insulation_Cold);
				result.insHeat = comp.GetStatValue(StatDefOf.Insulation_Heat);
				result.mass = comp.GetStatValue(StatDefOf.Mass);
				result.move = comp.GetStatValue(StatDefOf.MoveSpeed);
			}
			return result;
		}
	}

	public class PawnJSON : ThingJSON
	{
		public string name;
		//public string age;
		//public string gender;
		//public string bodyType;
		//public string storyTitle;
		//public trait[] traits;
		//public string childhood;
		//public string adulthood;
		public string healthState;
		//public string mentalState;
		//public string inspired;
		public CellJSON pos;
		public string rotation;
		public string posture;
		public bool drafted;
		//public thing aimingAt;
		public bool stunned;
		public bool controlable;
		//public container carry;
		//public container inventory;
		//public thing[] equipment;
		//public mind mind;
		//public verb[] verbs;
		//public need[] needs;
		//public skill[] skills;
		//public work[] work;
		//public job curJob;
		//public job[] queuedJobs;
		//public apparel[] apparel;
		//public bed bed;
		//public room room;
		//public relation[] relations;
		public string allowedArea;
		public string medCare;
		public string hostility;
		public bool selfTend;
		public bool fireAtWill;
		public string outfit;
		public string drugs;
		public string food;
		public string assignment;
		public string[] timetable;

		static IEnumerable<JobJSON> GetJobs(JobQueue queue)
		{
			if (queue == null) yield break;
			for (var i = 0; i < queue.Count; i++)
				yield return JobJSON.Make(queue[i].job);
		}

		static string ToAssignment(TimeAssignmentDef def)
		{
			var s = "";
			if (def != null && def.allowJoy) s += "J";
			if (def != null && def.allowRest) s += "R";
			return s;
		}

		public static PawnJSON Make(Verse.Pawn p)
		{
			if (p == null) return null;
			return new PawnJSON()
			{
				def = p.def.defName,
				hitPoints = p.HitPoints,

				name = p.Name.ToStringFull,
				//age = p.ageTracker?.AgeNumberString,
				//gender = p.gender.ToString(),
				//bodyType = p.story.bodyType.description,
				//storyTitle = p.story.TitleCap,
				//traits = p.story?.traits?.allTraits.Select(t => trait.make(t)).ToArray(),
				//childhood = p.story?.childhood?.title,
				//adulthood = p.story?.adulthood?.title,
				healthState = p.health?.State.ToString(),
				//mentalState = p.MentalState?.def.defName,
				//inspired = p.InspirationDef?.defName,
				pos = CellJSON.Make(p.Position),
				rotation = p.Rotation.ToStringHuman(),
				posture = p.jobs?.posture.ToString(),
				drafted = p.Drafted,
				//aimingAt = thing.make(p.TargetCurrentlyAimingAt.Thing),
				stunned = p.stances == null ? false : p.stances.stunner.Stunned,
				controlable = p.IsColonistPlayerControlled,
				//carry = container.make(p.carryTracker?.innerContainer),
				//inventory = container.make(p.inventory?.innerContainer),
				//equipment = p.equipment?.AllEquipmentListForReading.Select(t => thing.make(t)).ToArray(),
				//mind = mind.make(p.mindState),
				//verbs = p.verbTracker?.AllVerbs.Select(v2 => verb.make(v2)).ToArray(),
				//needs = p.needs?.AllNeeds.Select(n => need.make(n)).ToArray(),
				//skills = p.skills?.skills.Select(s => skill.make(s)).ToArray(),
				//work = Puppeteer.work.make(p.workSettings),
				//curJob = job.make(p.jobs?.curJob),
				//queuedJobs = getJobs(p.jobs?.jobQueue).ToArray(),
				//apparel = p.apparel?.WornApparel.Select(a => Puppeteer.apparel.make(a)).ToArray(),
				//bed = bed.make(p.ownership.OwnedBed),
				//room = room.make(p.ownership.OwnedRoom),
				//relations = p.relations?.DirectRelations.Select(d => relation.make(d)).ToArray(),
				allowedArea = p.playerSettings?.AreaRestriction?.Label,
				medCare = p.playerSettings?.medCare.GetLabel(),
				hostility = p.playerSettings?.hostilityResponse.GetLabel(),
				selfTend = p.playerSettings == null ? false : p.playerSettings.selfTend,
				fireAtWill = p.drafter.FireAtWill,
				outfit = p.outfits?.CurrentOutfit?.label,
				drugs = p.drugs?.CurrentPolicy?.label,
				food = p.foodRestriction?.CurrentFoodRestriction.label,
				assignment = ToAssignment(p.timetable?.CurrentAssignment),
				timetable = p.timetable?.times.Select(t => ToAssignment(t)).ToArray()
			};
		}
	}

	public class RelationJSON
	{
		public string other;
		public string type;

		public static RelationJSON Make(DirectPawnRelation d)
		{
			if (d == null) return null;
			return new RelationJSON()
			{
				other = d.otherPawn.Name.ToStringShort,
				type = d.def.defName
			};
		}
	}

	public class TraitJSON
	{
		public string name;
		public string[] disabled;

		public static TraitJSON Make(RimWorld.Trait t)
		{
			if (t == null) return null;
			return new TraitJSON()
			{
				name = t.LabelCap,
				disabled = t.GetDisabledWorkTypes().Select(d => d.defName).ToArray()
			};
		}
	}

	public class WorkJSON
	{
		public string name;
		public int prio;

		public static WorkJSON[] Make(Pawn_WorkSettings ws)
		{
			if (ws == null) return null;
			return ws.WorkGiversInOrderNormal.Select(wg =>
			{
				return new WorkJSON()
				{
					name = wg.def.defName,
					prio = ws.GetPriority(wg.def.workType)
				};
			}).ToArray();
		}
	}

	public class ContainerJSON
	{
		public List<ThingJSON> things;

		public static ContainerJSON Make(ThingOwner<Verse.Thing> thingOwner)
		{
			if (thingOwner == null) return null;
			return new ContainerJSON()
			{
				things = thingOwner
					.InnerListForReading
					.Select(t => ThingJSON.Make(t))
					.ToList()
			};
		}
	}

	public class MindJSON
	{
		public ThingJSON enemyTarget;
		public PawnJSON meleeThreat;
		public string prioWorkType;
		public CellJSON prioWorkCell;

		public static MindJSON Make(Pawn_MindState mindState)
		{
			if (mindState == null) return null;
			return new MindJSON()
			{
				meleeThreat = PawnJSON.Make(mindState.meleeThreat),
				enemyTarget = ThingJSON.Make(mindState.enemyTarget),
				prioWorkType = mindState.priorityWork?.WorkType?.defName,
				prioWorkCell = CellJSON.Make(mindState.priorityWork?.Cell)
			};
		}
	}

	public class VerbJSON
	{
		public string name;
		public string tool;
		public string state;

		public static VerbJSON Make(Verse.Verb v)
		{
			if (v == null) return null;
			return new VerbJSON()
			{
				name = v.GetDamageDef().defName,
				tool = v.tool.label,
				state = v.state.ToString()
			};
		}
	}

	public class NeedJSON
	{
		public string name;
		public float level;

		public static NeedJSON Make(RimWorld.Need n)
		{
			if (n == null) return null;
			return new NeedJSON()
			{
				name = n.def.defName,
				level = n.CurLevel
			};
		}
	}

	public class JobJSON
	{
		public string name;
		public bool forced;
		public VerbJSON verb;
		public BillJSON bill;

		public static JobJSON Make(Verse.AI.Job j)
		{
			if (j == null) return null;
			return new JobJSON()
			{
				name = j.def.defName,
				forced = j.playerForced,
				verb = VerbJSON.Make(j.verbToUse),
				bill = BillJSON.Make(j.bill)
			};
		}
	}

	public class SkillJSON
	{
		public string name;
		public int level;
		public int passion;

		public static SkillJSON Make(SkillRecord s)
		{
			if (s == null) return null;
			return new SkillJSON()
			{
				name = s.def.defName,
				level = s.Level,
				passion = s.TotallyDisabled ? -1 : (int)s.passion
			};
		}
	}

	public class BillJSON
	{
		public string name;
		public string recipe;
		public int minSkill;
		public int maxSkill;

		public static BillJSON Make(RimWorld.Bill b)
		{
			if (b == null) return null;
			return new BillJSON()
			{
				name = b.LabelCap,
				recipe = b.recipe.defName,
				minSkill = b.allowedSkillRange.min,
				maxSkill = b.allowedSkillRange.max
			};
		}
	}

	public class ApparelJSON : ThingJSON
	{
		public string name;
		public string desc;
		public bool fromCorpse;

		public static ApparelJSON Make(RimWorld.Apparel a)
		{
			if (a == null) return null;
			return new ApparelJSON()
			{
				name = a.def.defName,
				desc = a.GetInspectString(),
				fromCorpse = a.WornByCorpse
			};
		}
	}

	public class BedJSON
	{
		public string[] owners;
		public string quality;

		public static BedJSON Make(Building_Bed b)
		{
			if (b == null) return null;
			return new BedJSON()
			{
				owners = b.AssignedPawns.Select(o => o.Name.ToStringShort).ToArray(),
				quality = b.TryGetComp<CompQuality>()?.Quality.GetLabel()
			};
		}
	}

	public class RoomJSON
	{
		public string role;
		public float temp;
		public int size;
		public string[] owners;

		public static RoomJSON Make(Room r)
		{
			if (r == null) return null;
			return new RoomJSON()
			{
				role = r.Role.defName,
				temp = r.Temperature,
				size = r.CellCount,
				owners = r.Owners.Select(o => o.Name.ToStringShort).ToArray()
			};
		}
	}
}