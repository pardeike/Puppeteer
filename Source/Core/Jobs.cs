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

				default:
					Log.Warning($"unknown job method '{job.method}'");
					break;
			}
		}

		// get-attack-targets(melee=true/false)
		static object GetAttackTargets(Pawn pawn, string[] args)
		{
			const int maxDistance = 30;

			if (args.Length != 1) return "need-1-arg";
			var melee = bool.Parse(args[0]);

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
					name = $"{pair.First.LabelCap} ({Tools.GetDirectionalString(pawn, pair.First)})",
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
				var thingID = int.Parse(args[0]);
				var melee = bool.Parse(args[1]);
				if (pawn == null) return "no-colonist";
				var map = pawn.Map;
				if (map == null) return "no-map";

				if (pawn.Drafted == false)
					pawn.drafter.Drafted = true;

				var target = map.listerThings.AllThings.OfType<Pawn>().FirstOrDefault(p => p.thingIDNumber == thingID);

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
	}
}