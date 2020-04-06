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
	public enum Event
	{
		GameEntered,
		GameExited,
		Save,
		ColonistsChanged
	}

	public interface ICommandProcessor
	{
		void Message(byte[] msg);
	}

	[StaticConstructorOnStartup]
	public class Puppeteer : ICommandProcessor
	{
		public static Puppeteer instance = new Puppeteer();

		readonly Timer earnTimer = new Timer(earnIntervalInSeconds * 1000) { AutoReset = true };
		public Timer connectionRetryTimer = new Timer(10000) { AutoReset = true };

		const bool developmentMode = true;
		const int earnIntervalInSeconds = 2;
		const int earnAmount = 10;

		public Connection connection;
		readonly Viewers viewers;
		readonly Colonists colonists;
		bool firstTime = true;

		static Func<Pawn, bool, IEnumerable<IGrouping<BodyPartRecord, Hediff>>> VisibleHediffGroupsInOrder;

		public Puppeteer()
		{
			earnTimer.Elapsed += new ElapsedEventHandler((sender, e) =>
			{
				if (Find.CurrentMap != null)
					viewers.Earn(connection, earnAmount);
			});
			earnTimer.Start();

			viewers = new Viewers();
			colonists = new Colonists();

			connectionRetryTimer.Elapsed += new ElapsedEventHandler((sender, e) =>
			{
				connection?.Send(new Ping());
			});
			connectionRetryTimer.Start();

			var m_VisibleHediffGroupsInOrder = Method(typeof(HealthCardUtility), "VisibleHediffGroupsInOrder");
			VisibleHediffGroupsInOrder = (Func<Pawn, bool, IEnumerable<IGrouping<BodyPartRecord, Hediff>>>)Delegate.CreateDelegate(typeof(Func<Pawn, bool, IEnumerable<IGrouping<BodyPartRecord, Hediff>>>), m_VisibleHediffGroupsInOrder);
		}

		~Puppeteer()
		{
			connectionRetryTimer?.Stop();
			earnTimer?.Stop();
		}

		public void SetEvent(Event evt)
		{
			switch (evt)
			{
				case Event.GameEntered:
					connection = new Connection(developmentMode, this);
					break;
				case Event.GameExited:
					connection?.Disconnect();
					connection = null;
					break;
				case Event.Save:
					viewers.Save();
					colonists.Save();
					break;
				case Event.ColonistsChanged:
					if (firstTime == false)
						colonists.SendAllColonists(connection);
					firstTime = false;
					break;
			}
		}

		public ViewerInfo GetViewerInfo(Pawn pawn)
		{
			var colonist = colonists.FindColonist(pawn);
			if (colonist == null || colonist.controller == null) return null;
			var viewer = viewers.FindViewer(colonist.controller);
			if (viewer == null || viewer.controlling == null) return null;
			return new ViewerInfo()
			{
				controller = colonist.controller,
				pawn = viewer.controlling,
				connected = viewer.connected
			};
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
						colonists.SendAllColonists(connection);
						break;
					case "join":
						var join = Join.Create(msg);
						viewers.Join(connection, colonists, join.viewer);
						break;
					case "leave":
						var leave = Leave.Create(msg);
						viewers.Leave(leave.viewer);
						break;
					case "assign":
						var assign = Assign.Create(msg);
						colonists.Assign($"{assign.colonistID}", assign.viewer, connection);
						colonists.SendAllColonists(connection);
						break;
					case "state":
						var state = IncomingState.Create(msg);
						colonists.SetState(state);
						break;
					default:
						Log.Warning($"unknown command '{cmd.type}'");
						break;
				}
			}
			catch (System.Exception e)
			{
				Log.Warning($"While handling {msg}: {e}");
			}
		}

		public void PawnUnavailable(Pawn pawn)
		{
			colonists.Assign("" + pawn.thingIDNumber, null, connection);
		}

		public void PawnOnMap(Pawn pawn, byte[] image)
		{
			if (connection == null) return;
			var viewerInfo = GetViewerInfo(pawn);
			if (viewerInfo == null || viewerInfo.controller == null) return;
			connection.Send(new OnMap() { viewer = viewerInfo.controller, info = new OnMap.Info() { image = image } });
		}

		public void UpdatePortrait(Pawn pawn)
		{
			var colonist = colonists.FindColonist(pawn);
			if (colonist == null || colonist.controller == null) return;
			var viewer = viewers.FindViewer(colonist.controller);
			if (viewer == null || viewer.controlling == null) return;
			Viewers.SendPortrait(connection, viewer);
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
				var minMemory = thoughtGroup.First() as Thought_Memory;
				var maxMemory = thoughtGroup.Last() as Thought_Memory;
				var min = 0f;
				var max = 0f;
				if (minMemory != null)
				{
					var duration = overallThoughtGroup.def.DurationTicks;
					(duration - minMemory.age).TicksToPeriod(out var y1, out var q1, out var d1, out min);
					(duration - maxMemory.age).TicksToPeriod(out var y2, out var q2, out var d2, out max);
				}
				return new ColonistBaseInfo.ThoughtInfo() { name = name, value = value, min = (int)Math.Round(min), max = (int)Math.Round(max) };
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
				.Select((IEnumerable<Hediff> diffs) =>
				{
					var hediffInfos = new List<ColonistBaseInfo.HediffInfo>();
					var part = diffs.First<Hediff>().Part;
					var name = part.LabelCap;
					foreach (var grouping in from x in diffs group x by x.UIGroupKey)
					{
						ColonistBaseInfo.HediffInfo lastHediffInfo = null;
						if (grouping != null)
							foreach (var hediff2 in grouping)
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
							}
					}
					var color = HealthUtility.GetPartConditionLabel(pawn, part).Second;
					return new ColonistBaseInfo.Injury() { name = name, hediffs = hediffInfos.ToArray(), rgb = Tools.GetRGB(color) };
				})
				.ToArray();
		}

		static readonly FieldInfo f_skillDefsInListOrderCached = Field(typeof(SkillUI), "skillDefsInListOrderCached");
		static readonly FieldRef<List<SkillDef>> skillDefsInListOrderCachedRef = StaticFieldRefAccess<List<SkillDef>>(f_skillDefsInListOrderCached);
		public ColonistBaseInfo.SkillInfo[] GetSkills(Pawn pawn)
		{
			var skills = pawn.skills;
			return skillDefsInListOrderCachedRef()
				.Select(skillDef => skills.GetSkill(skillDef))
				.Select(skill =>
				{
					var name = skill.def.skillLabel.CapitalizeFirst();
					if (skill.TotallyDisabled)
						return new ColonistBaseInfo.SkillInfo() { name = name, level = -1 };
					return new ColonistBaseInfo.SkillInfo()
					{
						name = name,
						level = skill.Level,
						passion = (int)skill.passion,
						progress = new[] { (int)skill.xpSinceLastLevel, (int)skill.XpRequiredForLevelUp }
					};
				})
				.ToArray();
		}

		public void UpdateColonist(Pawn pawn)
		{
			var colonist = colonists.FindColonist(pawn);
			if (colonist == null || colonist.controller == null) return;
			var viewer = viewers.FindViewer(colonist.controller);
			if (viewer == null || viewer.controlling == null) return;

			var inspect = Array.Empty<string>();
			var info = new ColonistBaseInfo.Info();
			if (pawn.Spawned && pawn.Map != null)
			{
				inspect = pawn.GetInspectString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
				info.name = pawn.Name.ToStringFull;
				info.x = pawn.Position.x;
				info.y = pawn.Position.z;
				info.mx = pawn.Map.Size.x;
				info.my = pawn.Map.Size.z;
				info.inspect = inspect;
				info.health = new ColonistBaseInfo.Percentage()
				{
					label = HealthUtility.GetGeneralConditionLabel(pawn, true),
					percent = pawn.health.summaryHealth.SummaryHealthPercent
				};
				info.mood = new ColonistBaseInfo.Percentage()
				{
					label = pawn.needs.mood.MoodString.CapitalizeFirst(),
					percent = pawn.needs.mood.CurLevelPercentage
				};
				info.restrict = new ColonistBaseInfo.Value(pawn.timetable.CurrentAssignment.LabelCap, pawn.timetable.CurrentAssignment.color);
				info.area = new ColonistBaseInfo.Value(AreaUtility.AreaAllowedLabel(pawn), pawn.playerSettings?.EffectiveAreaRestriction?.Color ?? Color.gray);
				info.drafted = pawn.Drafted;
				info.response = pawn.playerSettings.hostilityResponse.GetLabel();
				info.needs = GetNeeds(pawn);
				info.thoughts = GetThoughts(pawn);
				info.capacities = GetCapacities(pawn);
				info.bleedingRate = (int)(pawn.health.hediffSet.BleedRateTotal * 100f + 0.5f);
				var ticksUntilDeath = HealthUtility.TicksUntilDeathDueToBloodLoss(pawn);
				info.deathIn = (int)(((float)ticksUntilDeath / GenDate.TicksPerHour) + 0.5f);
				info.injuries = GetInjuries(pawn);
				info.skills = GetSkills(pawn);
			}
			connection.Send(new ColonistBaseInfo() { viewer = viewer.vID, info = info });
		}
	}
}