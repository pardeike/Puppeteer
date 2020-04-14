using Newtonsoft.Json;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;
using Verse.AI;
using WebSocketSharp;

namespace Puppeteer
{
	public static class Jobs
	{
		public static void Run(Connection connection, Pawn pawn, IncomingJob job)
		{
			void RunOnQueue(Func<Pawn, string[], object> action)
			{
				var result = new OutgoingJobResult() { id = job.id, viewer = job.user };
				OperationQueue.Add(OperationType.Job, () =>
				{
					var obj = action(pawn, job.args);
					using (var writer = new StringWriter())
					{
						var serializer = new JsonSerializer();
						serializer.Serialize(writer, obj);
						result.info = writer.ToString();
					}
					connection.Send(result);
				});
			}

			switch (job.method)
			{
				case "get-attack-targets":
					RunOnQueue(GetAttackTargets);
					break;

				case "attack-target":
					RunOnQueue(AttackTarget);
					break;

				case "get-weapons":
					RunOnQueue(GetWeapons);
					break;

				case "select-weapon":
					RunOnQueue(SelectWeapon);
					break;

				case "get-rest":
					RunOnQueue(GetRest);
					break;

				case "do-rest":
					RunOnQueue(DoRest);
					break;

				case "get-tend":
					RunOnQueue(GetTend);
					break;

				case "do-tend":
					RunOnQueue(DoTend);
					break;

				default:
					Log.Warning($"unknown job method '{job.method}'");
					break;
			}
		}

		// get-attack-targets(melee=true/false)
		static object GetAttackTargets(Pawn pawn, string[] args)
		{
			const int maxDistance = 30;
			var emptyTargets = new AttackResult() { results = new List<AttackResult.Result>() };

			if (args.Length != 1) return "need-1-arg";
			var melee = bool.Parse(args[0]);

			if (pawn.WorkTagIsDisabled(WorkTags.Violent)) return "non-violent";
			if (Tools.CannotMoveOrDo(pawn)) return "no-action";
			if (melee == false && Tools.HasRangedAttack(pawn) == false) return "no-ranged-attack";

			var map = pawn.Map;
			var results = map?.attackTargetsCache
				.GetPotentialTargetsFor(pawn)
				.Where(target => target.ThreatDisabled(pawn) == false) // maybe skip this
				.OfType<Pawn>()
				.Distinct()
				.Select(enemy => new Pair<Pawn, int>(enemy, enemy.Position.DistanceToSquared(pawn.Position)))
				.Where(pair =>
				{
					var enemy = pair.First;
					var distance = pair.Second;
					if (distance > maxDistance * maxDistance) return false;
					return pawn.CanReach(enemy, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn);
				})
				.OrderBy(pair => pair.Second)
				.Select(pair => new AttackResult.Result()
				{
					name = $"{pair.First.LabelCap} ({Tools.GetPathingTime(pawn, pair.First.Position)}m {Tools.GetDirectionalString(pawn, pair.First)})",
					id = pair.First.thingIDNumber
				})
				.ToList() ?? new List<AttackResult.Result>();

			return new AttackResult() { results = results };
		}

		// attack-target(thingID=#,melee=true/false)
		static object AttackTarget(Pawn pawn, string[] args)
		{
			try
			{
				if (args.Length != 2) return "need-2-args";

				var target = Tools.GetThingFromArgs<Pawn>(pawn, args, 0);
				if (target == null) return "no-target";

				if (pawn.Drafted == false)
					pawn.drafter.Drafted = true;

				var melee = bool.Parse(args[1]);
				if (melee == false)
				{
					var giver = new FightEnemy(target);
					var thinkResult = giver.TryIssueJobPackage(pawn, new JobIssueParams());
					if (thinkResult.Job != null)
					{
						pawn.jobs.StartJob(thinkResult.Job, JobCondition.None, null, false, true, null, null, false, false);
						return "ok";
					}
					return "no-job-package";
				}

				var action = FloatMenuUtility.GetMeleeAttackAction(pawn, target, out var failed);
				if (action == null) return "no-job";
				action();
				return failed.IsNullOrEmpty() ? "ok" : failed;
			}
			catch
			{
				return "err";
			}
		}

		// get-weapons(best/near)
		static object GetWeapons(Pawn pawn, string[] args)
		{
			var map = pawn.Map;
			if (map == null) return "no-pawn";
			if (pawn.WorkTagIsDisabled(WorkTags.Violent)) return "no-violence";
			if (Tools.CannotMoveOrDo(pawn)) return "no-action";

			if (args.Length != 1) return "need-1-arg";
			var selector = args[0];

			var things = map.listerThings
				.ThingsInGroup(ThingRequestGroup.Weapon);

			if (selector == "best")
				things.Sort(new MarketValueSorter());
			if (selector == "near")
				things.Sort(new DistanceSorter(pawn.Position));

			var weapons = new List<Thing>();
			var knownDefs = new HashSet<ThingDef>();
			foreach (var thing in things)
			{
				if (knownDefs.Contains(thing.def) == false)
				{
					_ = knownDefs.Add(thing.def);
					weapons.Add(thing);
				}
			}

			var results = weapons.Select(thing => new ItemResult.Result()
			{
				name = $"{thing.LabelCap} ({Tools.GetPathingTime(pawn, thing.Position)}m {Tools.GetDirectionalString(pawn, thing)})",
				id = thing.thingIDNumber
			});
			return new ItemResult() { results = results.ToList() };
		}

		// select-weapon(thingID=#)
		static object SelectWeapon(Pawn pawn, string[] args)
		{
			var weapon = Tools.GetThingFromArgs<Thing>(pawn, args, 0);
			if (weapon == null) return "no";
			return pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, weapon), JobTag.Misc) ? "ok" : "no";
		}

		// get-rest()
		static object GetRest(Pawn pawn, string[] _args)
		{
			var map = pawn.Map;
			if (map == null) return "no-pawn";
			if (Tools.CannotMoveOrDo(pawn)) return "no-action";

			var bedInfos = map.listerThings
				.ThingsInGroup(ThingRequestGroup.Bed)
				.OfType<Building_Bed>()
				.Where(bed => bed.Medical)
				.Select(bed =>
				{
					var info = bed.LabelShortCap;
					var occupant = bed.CurOccupants.FirstOrDefault();
					if (occupant != null) info += $", used by {occupant.LabelShortCap}";
					return new Pair<Building_Bed, string>(bed, info);
				});

			var results = bedInfos.Select(pair =>
			{
				var bed = pair.First;
				var info = pair.Second;
				return new ItemResult.Result()
				{
					name = $"{info} ({Tools.GetPathingTime(pawn, bed.Position)}m {Tools.GetDirectionalString(pawn, bed)})",
					id = bed.thingIDNumber
				};
			});
			return new ItemResult() { results = results.ToList() };
		}

		// do-rest(thingID=#)
		static object DoRest(Pawn pawn, string[] args)
		{
			var bed = Tools.GetThingFromArgs<Building_Bed>(pawn, args, 0);
			if (bed == null) return "no-bed";
			if (pawn.Drafted)
				pawn.drafter.Drafted = false;
			return pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobTag.Misc) ? "ok" : "no";
		}

		// get-tend()
		static object GetTend(Pawn pawn, string[] _args)
		{
			if (pawn.WorkTagIsDisabled(WorkTags.Caring)) return "no-caring";
			if (Tools.CannotMoveOrDo(pawn)) return "no-action";

			var pawns = PlayerPawns.AllPawns(pawn.Map)
				.Where(p => p != pawn && p.health.HasHediffsNeedingTendByPlayer(false) && p.CurrentBed() != null);

			var results = pawns.Select(injured => new ItemResult.Result()
			{
				name = $"{injured.LabelCap} ({Tools.GetPathingTime(pawn, injured.Position)}m {Tools.GetDirectionalString(pawn, injured)})",
				id = injured.thingIDNumber
			});
			return new ItemResult() { results = results.ToList() };
		}

		// do-tend(thingID=#)
		static object DoTend(Pawn pawn, string[] args)
		{
			var injured = Tools.GetThingFromArgs<Pawn>(pawn, args, 0);
			if (injured == null) return "not-injured";
			if (pawn.Drafted)
				pawn.drafter.Drafted = false;
			return pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.TendPatient, injured), JobTag.Misc) ? "ok" : "no";
		}
	}
}