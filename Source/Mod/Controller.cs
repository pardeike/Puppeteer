using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace Puppeteer
{
	public enum PuppeteerEvent
	{
		GameEntered,
		GameExited,
		Save,
		ColonistsChanged,
		AreasChanged,
		PrioritiesChanged,
		SendChangedPriorities,
		SchedulesChanged,
		SendChangedSchedules,
		UpdateColonists,
		TimeChanged,
	}

	public interface ICommandProcessor
	{
		void Message(byte[] msg);
	}

	[StaticConstructorOnStartup]
	public class Controller : ICommandProcessor
	{
		public static Controller instance = new Controller();

		readonly Timer earnTimer = new Timer(earnIntervalInSeconds * 1000) { AutoReset = true };
		public Timer connectionRetryTimer = new Timer(10000) { AutoReset = true };

		const int earnIntervalInSeconds = 2;
		const int earnAmount = 10;

		public Connection connection;
		bool firstTime = true;
		bool prioritiesChanged = false;
		bool schedulesChanged = false;

		static readonly MethodInfo m_VisibleHediffGroupsInOrder = Method(typeof(HealthCardUtility), "VisibleHediffGroupsInOrder");
		static readonly Func<Pawn, bool, IEnumerable<IGrouping<BodyPartRecord, Hediff>>> VisibleHediffGroupsInOrder = (Func<Pawn, bool, IEnumerable<IGrouping<BodyPartRecord, Hediff>>>)Delegate.CreateDelegate(typeof(Func<Pawn, bool, IEnumerable<IGrouping<BodyPartRecord, Hediff>>>), m_VisibleHediffGroupsInOrder);

		public Controller()
		{
			_ = FileWatcher.AddListener((action, file) =>
			{
				if (file == Connection.tokenFilename)
				{
					Tools.LogWarning("Token file changed");
					var timer = new Timer(1000);
					timer.Elapsed += (_sender, _evnt) => connection?.TryConnect();
					timer.AutoReset = false;
					timer.Start();
				}
			});

			earnTimer.Elapsed += new ElapsedEventHandler((sender, e) =>
			{
				if (Find.CurrentMap != null)
					GeneralCommands.SendEarnToAll(connection, earnAmount);
			});
			earnTimer.Start();

			connectionRetryTimer.Elapsed += new ElapsedEventHandler((sender, e) =>
			{
				connection?.Send(new Ping());
			});
			connectionRetryTimer.Start();
		}

		~Controller()
		{
			connectionRetryTimer?.Stop();
			earnTimer?.Stop();
		}

		public void SetEvent(PuppeteerEvent evt)
		{
			try
			{
				// Log.Warning($"SET EVENT {evt}");
				switch (evt)
				{
					case PuppeteerEvent.GameEntered:
						connection = new Connection(this);
						break;
					case PuppeteerEvent.GameExited:
						connection?.Disconnect();
						connection = null;
						break;
					case PuppeteerEvent.Save:
						State.Save();
						break;
					case PuppeteerEvent.ColonistsChanged:
						if (firstTime == false)
							GeneralCommands.SendAllColonists(connection);
						firstTime = false;
						break;
					case PuppeteerEvent.AreasChanged:
						GeneralCommands.SendAreas(connection);
						break;
					case PuppeteerEvent.PrioritiesChanged:
						prioritiesChanged = true;
						break;
					case PuppeteerEvent.SendChangedPriorities:
						if (prioritiesChanged)
						{
							prioritiesChanged = false;
							GeneralCommands.SendPriorities(connection);
						}
						break;
					case PuppeteerEvent.SchedulesChanged:
						schedulesChanged = true;
						break;
					case PuppeteerEvent.SendChangedSchedules:
						if (schedulesChanged)
						{
							schedulesChanged = false;
							GeneralCommands.SendSchedules(connection);
						}
						break;
					case PuppeteerEvent.UpdateColonists:
						Tools.UpdateColonists();
						break;
					case PuppeteerEvent.TimeChanged:
						GeneralCommands.SendTimeInfoToAll();
						break;
				}
			}
			catch (Exception e)
			{
				Tools.LogWarning($"While setting event {evt}: {e}");
			}
		}

		public void Message(byte[] msg)
		{
			if (connection == null) return;
			try
			{
				var cmd = SimpleCmd.Create(msg);
				// Log.Warning($"MSG {cmd.type}");
				switch (cmd.type)
				{
					case "welcome":
					{
						var info = Welcome.Create(msg);
						GeneralCommands.CheckVersionRequired(info);
						GeneralCommands.SendAllColonists(connection);
						break;
					}
					case "join":
						GeneralCommands.Join(connection, Join.Create(msg).viewer);
						break;
					case "leave":
						GeneralCommands.Leave(Leave.Create(msg).viewer);
						break;
					case "assign":
					{
						var assign = Assign.Create(msg);
						var pawn = Tools.ColonistForThingID(assign.colonistID);
						AssignViewerToPawn(assign.viewer, pawn);
						break;
					}
					case "state":
					{
						var state = IncomingState.Create(msg);
						OperationQueue.Add(OperationType.SetState, () => StateCommand.Set(connection, state));
						break;
					}
					case "job":
					{
						var job = IncomingJob.Create(msg);
						var puppeteer = State.Instance.PuppeteerForViewer(job.user);
						Jobs.Run(connection, puppeteer, job);
						break;
					}
					case "stalling":
					{
						var stalling = StallingState.Create(msg);
						var puppeteer = State.Instance.PuppeteerForViewer(stalling.viewer);
						if (puppeteer != null)
						{
							puppeteer.stalling = stalling.state;
							var state = puppeteer.stalling ? "started" : "ends";
							Tools.LogWarning($"{stalling.viewer.name} {state} stalling");
						}
						break;
					}
					default:
					{
						Tools.LogWarning($"unknown command '{cmd.type}'");
						break;
					}
				}
			}
			catch (Exception e)
			{
				Tools.LogWarning($"While handling {msg}: {e}");
			}
		}

		public void PawnAvailable(Pawn pawn)
		{
			State.Instance.UpdatePawn(pawn);
			State.Save();
			GeneralCommands.SendAllColonists(connection);
		}

		public void PawnUnavailable(Pawn pawn)
		{
			GeneralCommands.Assign(connection, pawn, null);
			if (State.Instance.RemovePawn(pawn))
				State.Save();
			GeneralCommands.SendAllColonists(connection);
		}

		public void AssignViewerToPawn(ViewerID vID, Pawn pawn)
		{
			GeneralCommands.Assign(connection, pawn, vID);
			GeneralCommands.SendAllColonists(connection);
		}

		public void UpdatePortrait(Pawn pawn)
		{
			var puppet = State.Instance.PuppetForPawn(pawn);
			GeneralCommands.SendPortrait(connection, puppet?.puppeteer);
		}

		ColonistBaseInfo.NeedInfo[] GetNeeds(Pawn pawn)
		{
			var needs = pawn.needs.AllNeeds.Where(n => n.ShowOnNeedList && n.GetType() != typeof(Need_Mood)).ToList();
			if (needs == null) return Array.Empty<ColonistBaseInfo.NeedInfo>();
			PawnNeedsUIUtility.SortInDisplayOrder(needs);
			needs.InsertRange(0, pawn.needs.AllNeeds.Where(n => n.GetType() == typeof(Need_Mood)));
			return needs
				.Select(need => new ColonistBaseInfo.NeedInfo(need))
				.ToArray();
		}

		ColonistBaseInfo.ThoughtInfo[] GetThoughts(Pawn pawn)
		{
			var overallThoughtGroups = new List<Thought>();
			PawnNeedsUIUtility.GetThoughtGroupsInDisplayOrder(pawn.needs.mood, overallThoughtGroups);
			return overallThoughtGroups.Select(overallThoughtGroup =>
			{
				var thoughtGroup = new List<Thought>();
				pawn.needs.mood.thoughts.GetMoodThoughts(overallThoughtGroup, thoughtGroup);
				var leadingThoughtInGroup = PawnNeedsUIUtility.GetLeadingThoughtInGroup(thoughtGroup);
				if (!leadingThoughtInGroup.VisibleInNeedsTab)
					return null;
				var name = leadingThoughtInGroup.LabelCap;
				if (thoughtGroup.Count > 1) name = $"{name} {thoughtGroup.Count}x";
				var value = (int)pawn.needs.mood.thoughts.MoodOffsetOfGroup(leadingThoughtInGroup);
				var duration = overallThoughtGroup.def.DurationTicks;
				var memories = thoughtGroup.OfType<Thought_Memory>().Where(th => th.age > 0);
				var min = memories.Any() ? (int)Math.Round(memories.Min(thought =>
				{
					(duration - thought.age).TicksToPeriod(out var y1, out var q1, out var d1, out var val);
					return val;
				})) : 0;
				var max = memories.Any() ? (int)Math.Round(memories.Max(thought =>
				{
					(duration - thought.age).TicksToPeriod(out var y1, out var q1, out var d1, out var val);
					return val;
				})) : 0;
				return new ColonistBaseInfo.ThoughtInfo() { name = name, value = value, min = min, max = max };
			})
			.ToArray();
		}

		ColonistBaseInfo.CapacityInfo[] GetCapacities(Pawn pawn)
		{
			var result = DefDatabase<PawnCapacityDef>.AllDefs
				.Where(def => def.showOnHumanlikes && PawnCapacityUtility.BodyCanEverDoCapacity(pawn.RaceProps.body, def))
				.OrderBy(def => def.listOrder)
				.Select(def =>
				{
					var name = def.GetLabelFor(pawn.RaceProps.IsFlesh, pawn.RaceProps.Humanlike).CapitalizeFirst();
					var value = HealthCardUtility.GetEfficiencyLabel(pawn, def);
					return new ColonistBaseInfo.CapacityInfo() { name = name, value = value.First, rgb = Tools.GetRGB(value.Second) };
				})
				.ToList();
			var pain = HealthCardUtility.GetPainLabel(pawn);
			result.Insert(0, new ColonistBaseInfo.CapacityInfo()
			{
				name = "PainLevel".Translate(),
				value = pain.First,
				rgb = Tools.GetRGB(pain.Second)
			});
			return result.ToArray();
		}

		ColonistBaseInfo.Injury[] GetInjuries(Pawn pawn)
		{
			return VisibleHediffGroupsInOrder(pawn, true)
				.Cast<IEnumerable<Hediff>>()
				.Select(diffs =>
				{
					var hediffInfos = new List<ColonistBaseInfo.HediffInfo>();
					var part = diffs.First().Part;
					var name = part?.LabelCap ?? "WholeBody".Translate();
					diffs.GroupBy(d => d.UIGroupKey).DoIf(grouping => grouping != null, grouping =>
					{
						ColonistBaseInfo.HediffInfo lastHediffInfo = null;
						grouping.DoIf(hediff2 => hediff2 != null, hediff2 =>
						{
							if (hediff2.LabelCap != lastHediffInfo?.name)
							{
								lastHediffInfo = new ColonistBaseInfo.HediffInfo()
								{
									name = hediff2.LabelCap,
									count = 1,
									rgb = Tools.GetRGB(hediff2.LabelColor)
								};
								hediffInfos.Add(lastHediffInfo);
							}
							else
							{
								if (lastHediffInfo != null)
									lastHediffInfo.count++;
							}
						});
					});
					var color = part == null ? HealthUtility.RedColor : HealthUtility.GetPartConditionLabel(pawn, part).Second;
					return new ColonistBaseInfo.Injury() { name = name, hediffs = hediffInfos.ToArray(), rgb = Tools.GetRGB(color) };
				})
				.ToArray();
		}

		static readonly FieldInfo f_skillDefsInListOrderCached = Field(typeof(SkillUI), "skillDefsInListOrderCached");
		static readonly FieldRef<List<SkillDef>> skillDefsInListOrderCachedRef = StaticFieldRefAccess<List<SkillDef>>(f_skillDefsInListOrderCached);
		public static ColonistBaseInfo.SkillInfo[] GetSkills(Pawn pawn)
		{
			var skills = pawn.skills;
			return skillDefsInListOrderCachedRef()
				.Select(skillDef => skills.GetSkill(skillDef))
				.Where(skill => skill.TotallyDisabled == false)
				.Select(skill => new ColonistBaseInfo.SkillInfo()
				{
					name = skill.def.skillLabel.CapitalizeFirst(),
					level = skill.Level,
					passion = (int)skill.passion,
					progress = new[] { (int)skill.xpSinceLastLevel, (int)skill.XpRequiredForLevelUp }
				})
				.ToArray();
		}

		static readonly MethodInfo m_GetWorkTypeDisabledCausedBy = Method(typeof(CharacterCardUtility), "GetWorkTypeDisabledCausedBy");
		static readonly FastInvokeHandler GetWorkTypeDisabledCausedBy = MethodInvoker.GetHandler(m_GetWorkTypeDisabledCausedBy);

		static readonly MethodInfo m_GetWorkTypesDisabledByWorkTag = Method(typeof(CharacterCardUtility), "GetWorkTypesDisabledByWorkTag");
		static readonly FastInvokeHandler GetWorkTypesDisabledByWorkTag = MethodInvoker.GetHandler(m_GetWorkTypesDisabledByWorkTag);

		public void UpdateColonist(State.Puppeteer puppeteer)
		{
			var pawn = puppeteer?.puppet?.pawn;
			if (pawn == null) return;

			string IncapableInfo(WorkTags t)
			{
				return GetWorkTypeDisabledCausedBy(null, new object[] { pawn, t }) + "\n" + GetWorkTypesDisabledByWorkTag(null, new object[] { t });
			}

			var carrier = Tools.GetCarrier(pawn);
			var childhood = pawn.story.GetBackstory(BackstorySlot.Childhood);
			var adulthood = pawn.story.GetBackstory(BackstorySlot.Adulthood);
			var disabledTags = pawn.CombinedDisabledWorkTags.GetAllSelectedItems<WorkTags>().Where(t => t != WorkTags.None).ToList();
			var info = new ColonistBaseInfo.Info
			{
				name = pawn.Name.ToStringFull,
				x = (carrier ?? pawn).Position.x,
				y = (carrier ?? pawn).Position.z,
				mx = (carrier ?? pawn).Map?.Size.x ?? 0,
				my = (carrier ?? pawn).Map?.Size.z ?? 0,
				childhood = new Tag(childhood?.TitleCapFor(pawn.gender) ?? "", childhood?.FullDescriptionFor(pawn) ?? ""),
				adulthood = new Tag(adulthood?.TitleCapFor(pawn.gender) ?? "", adulthood?.FullDescriptionFor(pawn) ?? ""),
				inspect = carrier != null ? Array.Empty<string>() : pawn.GetInspectString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None),
				health = new ColonistBaseInfo.Percentage()
				{
					label = HealthUtility.GetGeneralConditionLabel(pawn, true),
					percent = pawn.health.summaryHealth.SummaryHealthPercent
				},
				mood = new ColonistBaseInfo.Percentage()
				{
					label = pawn.needs.mood.MoodString.CapitalizeFirst(),
					percent = pawn.needs.mood.CurLevelPercentage
				},
				restrict = new ColonistBaseInfo.Value(pawn.timetable.CurrentAssignment.LabelCap, pawn.timetable.CurrentAssignment.color),
				area = new ColonistBaseInfo.Value(AreaUtility.AreaAllowedLabel(pawn), pawn.playerSettings?.EffectiveAreaRestriction?.Color ?? Color.gray),
				drafted = pawn.Drafted,
				response = pawn.playerSettings?.hostilityResponse.GetLabel() ?? "",
				needs = GetNeeds(pawn),
				thoughts = GetThoughts(pawn),
				capacities = GetCapacities(pawn),
				bleedingRate = (int)(pawn.health.hediffSet.BleedRateTotal * 100f + 0.5f)
			};
			var ticksUntilDeath = HealthUtility.TicksUntilDeathDueToBloodLoss(pawn);
			info.deathIn = (int)(((float)ticksUntilDeath / GenDate.TicksPerHour) + 0.5f);
			info.injuries = GetInjuries(pawn);
			info.skills = GetSkills(pawn);
			info.incapable = disabledTags.Select(tag => new Tag(tag.LabelTranslated().CapitalizeFirst(), IncapableInfo(tag))).ToArray();
			info.traits = pawn.story.traits.allTraits.Select(trait => new Tag(trait.LabelCap, trait.TipString(pawn))).ToArray();

			connection.Send(new ColonistBaseInfo() { viewer = puppeteer.vID, info = info });
		}
	}
}