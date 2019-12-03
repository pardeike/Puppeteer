using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using static Harmony.AccessTools;

namespace Puppeteer
{
	public class colonist
	{
		public pawn pawn;

		public colonist(Pawn p)
		{
			pawn = pawn.make(p);
		}
	}

	public class cell
	{
		public int x;
		public int z;

		public static cell make(IntVec3? c)
		{
			if (c == null) return null;
			return new cell()
			{
				x = c.Value.x,
				z = c.Value.z
			};
		}
	}

	public class thing
	{
		public string def;
		public int hitPoints;
		public string quality;
		public float beauty = -1;
		public float insCold = -1;
		public float insHeat = -1;
		public float mass = -1;
		public float move = -1;

		public static thing make(Thing thing)
		{
			if (thing == null) return null;
			if (thing is Pawn) return pawn.make(thing);
			var result = new thing()
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

	public class pawn: thing
	{
		public string name;
		public string age;
		public string gender;
		public string bodyType;
		public string storyTitle;
		public trait[] traits;
		public string childhood;
		public string adulthood;
		public string healthState;
		public string mentalState;
		public string inspired;
		public cell pos;
		public string rotation;
		public string posture;
		public bool drafted;
		public thing aimingAt;
		public bool stunned;
		public bool controlable;
		public container carry;
		public container inventory;
		public thing[] equipment;
		public mind mind;
		public verb[] verbs;
		public need[] needs;
		public skill[] skills;
		public work[] work;
		public job curJob;
		public job[] queuedJobs;
		public apparel[] apparel;
		public bed bed;
		public room room;
		public relation[] relations;
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

		static IEnumerable<job> getJobs(JobQueue queue)
		{
			if (queue == null) yield break;
			for (var i = 0; i < queue.Count; i++)
				yield return job.make(queue[i].job);
		}

		static string toAssignment(TimeAssignmentDef def)
		{
			var s = "";
			if (def != null && def.allowJoy) s += "J";
			if (def != null && def.allowRest) s += "R";
			return s;
		}

		public static pawn make(Pawn p) 
		{
			if (p == null) return null;
			return new pawn()
			{
				def = p.def.defName,
				hitPoints = p.HitPoints,

				name = p.Name.ToStringFull,
				age = p.ageTracker?.AgeNumberString,
				gender = p.gender.ToString(),
				bodyType = p.story.bodyType.description,
				storyTitle = p.story.TitleCap,
				traits = p.story?.traits?.allTraits.Select(t => trait.make(t)).ToArray(),
				childhood = p.story?.childhood?.title,
				adulthood = p.story?.adulthood?.title,
				healthState = p.health?.State.ToString(),
				mentalState = p.MentalState?.def.defName,
				inspired = p.InspirationDef?.defName,
				pos = cell.make(p.Position),
				rotation = p.Rotation.ToStringHuman(),
				posture = p.jobs?.posture.ToString(),
				drafted = p.Drafted,
				aimingAt = thing.make(p.TargetCurrentlyAimingAt.Thing),
				stunned = p.stances == null ? false : p.stances.stunner.Stunned,
				controlable = p.IsColonistPlayerControlled,
				carry = container.make(p.carryTracker?.innerContainer),
				inventory = container.make(p.inventory?.innerContainer),
				equipment = p.equipment?.AllEquipmentListForReading.Select(t => thing.make(t)).ToArray(),
				mind = mind.make(p.mindState),
				verbs = p.verbTracker?.AllVerbs.Select(v2 => verb.make(v2)).ToArray(),
				needs = p.needs?.AllNeeds.Select(n => need.make(n)).ToArray(),
				skills = p.skills?.skills.Select(s => skill.make(s)).ToArray(),
				work = Puppeteer.work.make(p.workSettings),
				curJob = job.make(p.jobs?.curJob),
				queuedJobs = getJobs(p.jobs?.jobQueue).ToArray(),
				apparel = p.apparel?.WornApparel.Select(a => Puppeteer.apparel.make(a)).ToArray(),
				bed = bed.make(p.ownership.OwnedBed),
				room = room.make(p.ownership.OwnedRoom),
				relations = p.relations?.DirectRelations.Select(d => relation.make(d)).ToArray(),
				allowedArea = p.playerSettings?.AreaRestriction?.Label,
				medCare = p.playerSettings?.medCare.GetLabel(),
				hostility = p.playerSettings?.hostilityResponse.GetLabel(),
				selfTend = p.playerSettings == null ? false : p.playerSettings.selfTend,
				fireAtWill = p.drafter.FireAtWill,
				outfit = p.outfits?.CurrentOutfit?.label,
				drugs = p.drugs?.CurrentPolicy?.label,
				food = p.foodRestriction?.CurrentFoodRestriction.label,
				assignment = toAssignment(p.timetable?.CurrentAssignment),
				timetable = p.timetable?.times.Select(t => toAssignment(t)).ToArray()
			};
		}
	}

	public class relation
	{
		public string other;
		public string type;

		public static relation make(DirectPawnRelation d)
		{
			if (d == null) return null;
			return new relation()
			{
				other = d.otherPawn.Name.ToStringShort,
				type = d.def.defName
			};
		}
	}

	public class trait
	{
		public string name;
		public string[] disabled;

		public static trait make(Trait t)
		{
			if (t == null) return null;
			return new trait()
			{
				name = t.LabelCap,
				disabled = t.GetDisabledWorkTypes().Select(d => d.defName).ToArray()
			};
		}
	}

	public class work
	{
		public string name;
		public int prio;

		public static work[] make(Pawn_WorkSettings ws)
		{
			if (ws == null) return null;
			return ws.WorkGiversInOrderNormal.Select(wg =>
			{
				return new work()
				{
					name = wg.def.defName,
					prio = ws.GetPriority(wg.def.workType)
				};
			}).ToArray();
		}
	}

	public class container
	{
		public List<thing> things;

		public static container make(ThingOwner<Thing> thingOwner)
		{
			if (thingOwner == null) return null;
			return new container()
			{
				things = thingOwner
					.InnerListForReading
					.Select(t => thing.make(t))
					.ToList()
			};
		}
	}

	public class mind
	{
		public thing enemyTarget;
		public pawn meleeThreat;
		public string prioWorkType;
		public cell prioWorkCell;

		public static mind make(Pawn_MindState mindState)
		{
			if (mindState == null) return null;
			return new mind()
			{
				meleeThreat = pawn.make(mindState.meleeThreat),
				enemyTarget = thing.make(mindState.enemyTarget),
				prioWorkType = mindState.priorityWork?.WorkType?.defName,
				prioWorkCell = cell.make(mindState.priorityWork?.Cell)
			};
		}
	}

	public class verb
	{
		public string name;
		public string tool;
		public string state;

		public static verb make(Verb v)
		{
			if (v == null) return null;
			return new verb()
			{
				name = v.GetDamageDef().defName,
				tool = v.tool.label,
				state = v.state.ToString()
			};
		}
	}

	public class need
	{
		public string name;
		public float level;

		public static need make(Need n)
		{
			if (n == null) return null;
			return new need()
			{
				name = n.def.defName,
				level = n.CurLevel
			};
		}
	}

	public class job
	{
		public string name;
		public bool forced;
		public verb verb;
		public bill bill;

		public static job make(Job j)
		{
			if (j == null) return null;
			return new job()
			{
				name = j.def.defName,
				forced = j.playerForced,
				verb = verb.make(j.verbToUse),
				bill = bill.make(j.bill)
			};
		}
	}

	public class skill
	{
		public string name;
		public int level;
		public int passion;

		public static skill make(SkillRecord s)
		{
			if (s == null) return null;
			return new skill()
			{
				name = s.def.defName,
				level = s.Level,
				passion = s.TotallyDisabled ? -1 : (int)s.passion
			};
		}
	}

	public class bill
	{
		public string name;
		public string recipe;
		public int minSkill;
		public int maxSkill;

		public static bill make(Bill b)
		{
			if (b == null) return null;
			return new bill()
			{
				name = b.LabelCap,
				recipe = b.recipe.defName,
				minSkill = b.allowedSkillRange.min,
				maxSkill = b.allowedSkillRange.max
			};
		}
	}

	public class apparel: thing
	{
		public string name;
		public string desc;
		public bool fromCorpse;

		public static apparel make(Apparel a)
		{
			if (a == null) return null;
			return new apparel()
			{
				name = a.def.defName,
				desc = a.GetInspectString(),
				fromCorpse = a.WornByCorpse
			};
		}
	}

	public class bed
	{
		public string[] owners;
		public string quality;

		public static bed make(Building_Bed b)
		{
			if (b == null) return null;
			return new bed()
			{
				owners = b.AssignedPawns.Select(o => o.Name.ToStringShort).ToArray(),
				quality = b.TryGetComp<CompQuality>()?.Quality.GetLabel()
			};
		}
	}

	public class room
	{
		public string role;
		public float temp;
		public int size;
		public string[] owners;

		public static room make(Room r)
		{
			if (r == null) return null;
			return new room()
			{
				role = r.Role.defName,
				temp = r.Temperature,
				size = r.CellCount,
				owners = r.Owners.Select(o => o.Name.ToStringShort).ToArray()
			};
		}
	}
}