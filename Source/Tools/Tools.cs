using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class Tools
	{
		public static bool IsLocalDev { get; } = File.Exists(Path.Combine(GenFilePaths.ConfigFolderPath, "PuppeteerLocalDevelopment.txt"));

		static Tools()
		{
			RoundRobbin.Create("update-colonist", 5f);
			RoundRobbin.Create("render-colonist", 30f);
		}

		public static string Base64Decode(this string value)
		{
			value = value.Replace('-', '+');
			value = value.Replace('_', '/');

			value = value.PadRight(value.Length + (4 - value.Length % 4) % 4, '=');

			var data = Convert.FromBase64String(value);
			return Encoding.UTF8.GetString(data);
		}

		public static int? SafeParse(string str)
		{
			try
			{
				return int.Parse(str);
			}
			catch
			{
				return null;
			}
		}

		public static int[] SafeParse(object obj, int count, string delim = ":")
		{
			try
			{
				var str = Convert.ToString(obj);
				if (str == null) return null;
				var vals = str.Split(delim.ToCharArray());
				if (vals.Length != count) return null;
				return vals.Select(v => int.Parse(v)).ToArray();
			}
			catch
			{
				return null;
			}
		}

		public static T Box<T>(T val, T min, T max) where T : IComparable
		{
			if (val.CompareTo(min) == -1) return min;
			if (val.CompareTo(max) == 1) return max;
			return val;
		}

		public static string GetModVersionString()
		{
			var assembly = Assembly.GetAssembly(typeof(Tools));
			var t_attribute = typeof(AssemblyFileVersionAttribute);
			var attribute = (AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(assembly, t_attribute, false);
			return attribute.Version;
		}

		public static void LogWarning(string message)
		{
			message = message
				.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
				.Select(line => Regex.Replace(line, @" \[0x[0-9a-fA-F]+\] in <[0-9a-fA-F]+>:\d+ ", ""))
				.Join(null, "\n");
			PuppetCommentator.Say(message);
			OperationQueue.Add(OperationType.Log, () =>
			{
				Log.Warning(message);
			});
		}

		public static void LogError(string message)
		{
			message = message.Split('\n', '\r').Select(line => Regex.Replace(line, @" \[0x[0-9a-fA-F]+\] in <[0-9a-fA-F]+>:\d+ ", "")).Join(null, "\n");
			PuppetCommentator.Say($"Error: {message}");
			OperationQueue.Add(OperationType.Log, () =>
			{
				Log.Error(message);
			});
		}

		public static int[] GetRGB(Color color)
		{
			return new[] { (int)(255 * color.r), (int)(255 * color.g), (int)(255 * color.b) };
		}

		public static string AsString(this DateTime dateTime)
		{
			return dateTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
		}

		public static string OriginalName(this Pawn pawn)
		{
			if (pawn == null || pawn.Spawned == false) return "";
			var name = pawn.Name;
			if (name is NameTriple triple)
				return $"{triple.First} {triple.Last}";
			return name.ToStringShort;
		}

		public static DateTime AsDateTime(this string date)
		{
			return DateTime.ParseExact(date, "yyyy'-'MM'-'dd' 'HH':'mm':'ss", CultureInfo.InvariantCulture);
		}

		public static string ReadConfig(this string name)
		{
			var path = Path.Combine(GenFilePaths.ConfigFolderPath, name);
			if (File.Exists(path) == false) return null;
			return File.ReadAllText(path, Encoding.UTF8);
		}

		public static void WriteConfig(this string name, string contents)
		{
			var path = Path.Combine(GenFilePaths.ConfigFolderPath, name);
			File.WriteAllText(path, contents);
		}

		public static Pawn ColonistForThingID(int thingID)
		{
			return Find.Maps
				.SelectMany(map => PlayerPawns.FreeColonists(map, false))
				.FirstOrDefault(pawn => pawn.thingIDNumber == thingID);
		}

		static readonly MethodInfo m_HasRangedAttack = AccessTools.Method(typeof(AttackTargetFinder), "HasRangedAttack");
		static readonly FastInvokeHandler d_HasRangedAttack = MethodInvoker.GetHandler(m_HasRangedAttack);
		public static bool HasRangedAttack(Pawn pawn)
		{
			return (bool)d_HasRangedAttack(null, new object[] { pawn });
		}

		public static bool CannotMoveOrDo(Pawn pawn)
		{
			return pawn == null
				|| pawn.IsColonistPlayerControlled == false
				|| pawn.Downed
				|| pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) == false
				|| pawn.InMentalState;
		}

		public static Pawn GetCarrier(Pawn pawn)
		{
			if (pawn == null) return null;
			if (pawn.Spawned) return null;
			var thingID = pawn.thingIDNumber;
			return Find.Maps.SelectMany(map => map.mapPawns.AllPawns)
				.FirstOrDefault(carrier => carrier?.carryTracker?.CarriedThing?.thingIDNumber == thingID);
		}

		public static void SetCurrentMapDirectly(Map map)
		{
			var game = Current.Game;
			game.currentMapIndex = (sbyte)game.Maps.IndexOf(map);
		}

		public static float CurrentMapOffset()
		{
			return 2000f * (1 + Current.Game.currentMapIndex);
		}

		static readonly string[] directions16 = new[]
		{
			 "E", "E-SE", "SE", "S-SE", "S", "S-SW", "SW", "W-SW",
			 "W", "W-NW", "NW", "N-NW", "N", "N-NE", "NE", "E-NE",
		};
		public static string GetDirectionalString(Thing from, Thing to)
		{
			var p1 = from.Position.ToVector3();
			var p2 = to.Position.ToVector3();
			var a = (int)(360 + p1.AngleToFlat(p2) + 11.25f) % 360;
			var i = (int)(a / 22.5f);
			return i < 0 || i > 15 ? "?" : directions16[i];
		}

		public static T GetThingFromArgs<T>(Pawn pawn, string[] args, int idx) where T : Thing
		{
			var map = pawn?.Map;
			if (map == null) return null;
			var thingID = int.Parse(args[idx]);
			return map.listerThings.AllThings.OfType<T>().FirstOrDefault(p => p.thingIDNumber == thingID) as T;
		}

		public static double GetPathingTime(Pawn pawn, IntVec3 destination)
		{
			if (pawn == null) return 0;
			var pos = pawn.Position;
			var traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassDoors, false);
			var path = pawn.Map.pathFinder.FindPath(pawn.Position, destination, traverseParams, PathEndMode.Touch);
			if (path == PawnPath.NotFound) return 999999;
			var cost = path.TotalCost;
			var min = Math.Floor(cost * 60f / GenDate.TicksPerHour);
			path.ReleaseToPool();
			return min;
		}

		public static IEnumerable<int> EqualSpreading(int tickInterval, long spreadCounter, int totalCount)
		{
			for (var i = 0; i < totalCount; i++)
			{
				var offset = totalCount > 1 ? i + i * ((tickInterval - totalCount) / totalCount) : 0;
				if (offset % tickInterval == spreadCounter % tickInterval)
					yield return i;
			}
		}

		public static void GameInit()
		{
			AllColonists(true, null).Do(pawn => State.Instance.UpdatePawn(pawn));
			State.Save();
		}

		public static List<Command> GetCommands(ISelectable selectable)
		{
			var saved = new List<object>(Find.Selector.SelectedObjects);
			try
			{
				Find.Selector.SelectedObjects.Clear();
				Find.Selector.SelectedObjects.Add(selectable);
				return selectable.GetGizmos()
					.Cast<Command>()
					.ToList();
			}
			catch (Exception)
			{
				return new List<Command>();
			}
			finally
			{
				Find.Selector.SelectedObjects.Clear();
				Find.Selector.SelectedObjects.AddRange(saved);
			}
		}

		public static string Restricted(Pawn pawn, IntVec3 cell, string text)
		{
			var settings = PawnSettings.SettingsFor(pawn);
			if (settings.enabled == false) return "Disabled";
			var restriction = settings.activeAreas
				.FirstOrDefault(area => (area.innerGrid.TrueCount == 0 || area.innerGrid[cell]) && area.restrictions.Any(res => res.IsRestricted(text)));
			return restriction?.label;
		}

		public static List<Pawn> AllColonists(bool forceUpdate, Map forMap = null)
		{
			var colonists = new List<Pawn>();
			Find.Maps.DoIf(map => forMap == null || map == forMap, map =>
			{
				var pawns = PlayerPawns.FreeColonists(map, forceUpdate);
				PlayerPawnsDisplayOrderUtility.Sort(pawns);
				colonists.AddRange(pawns);
			});
			if (forMap == null)
				Find.WorldObjects.Caravans
					.Where(caravan => caravan.IsPlayerControlled)
					.OrderBy(caravan => caravan.ID).Do(caravan =>
					{
						var pawns = caravan.PawnsListForReading;
						PlayerPawnsDisplayOrderUtility.Sort(pawns);
						colonists.AddRange(pawns);
					});
			return colonists;
		}

		public static void SetColonistNickname(Pawn pawn, string nick)
		{
			if (pawn == null || pawn.Spawned == false) return;
			var name3 = pawn.Name as NameTriple;
			if (name3 != null)
				pawn.Name = new NameTriple(name3.First, nick ?? name3.First, name3.Last);
		}

		public static void UpdateColonists()
		{
			var puppeteer = RoundRobbin.NextColonist("update-colonist");
			if (puppeteer != null)
				Controller.instance.UpdateColonist(puppeteer);
		}

		public static void AutoExposeDataWithDefaults<T>(this T settings) where T : new()
		{
			var defaults = new T();
			AccessTools.GetFieldNames(settings).Do(name =>
			{
				var finfo = AccessTools.Field(settings.GetType(), name);
				var value = finfo.GetValue(settings);
				var type = value.GetType();
				var defaultValue = Traverse.Create(defaults).Field(name).GetValue();
				var m_Look = AccessTools.Method(typeof(Scribe_Values), "Look", null, new Type[] { type });
				var arguments = new object[] { value, name, defaultValue, false };
				_ = m_Look.Invoke(null, arguments);
				finfo.SetValue(settings, arguments[0]);
			});
		}

		public static string SafeTranslate(this string key, params NamedArgument[] args)
		{
			if (key == null) return "";
			return key.Translate(args);
		}

		public static string TranslateHoursToText(float hours)
		{
			var ticks = (int)(GenDate.TicksPerHour * hours);
			return ticks.ToStringTicksToPeriodVerbose(true, false);
		}

		public static void Draw(this Texture2D texture, Rect rect, bool withAlpha = false, Color? color = null)
		{
			var c = color ?? Color.white;
			GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, withAlpha, 0f, c, Vector4.zero, Vector4.zero);
		}

		public static string GetModRootDirectory()
		{
			var me = LoadedModManager.GetMod<Puppeteer>();
			if (me == null)
			{
				Log.Error("LoadedModManager.GetMod<Puppeteer>() failed");
				return "";
			}
			return me.Content.RootDir;
		}

		public static OffLimitsArea GetSelectedArea()
		{
			return (Find.DesignatorManager.SelectedDesignator as Designator_OffLimits)?.area;
		}

		public static void SetCurrentOffLimitsDesignator(OffLimitsArea area = null, bool? mode = null)
		{
			var designatorManager = Find.DesignatorManager;
			var des = designatorManager.SelectedDesignator as Designator_OffLimits;

			if (area == null)
			{
				if (des != null && des.mode.HasValue == false)
					designatorManager.Deselect();
				return;
			}

			if (des == null || des.area != area || des.mode != mode)
			{
				var mStr = mode.HasValue == false ? "null" : (mode.Value ? "true" : "false");
				designatorManager.Select(new Designator_OffLimits(area, mode));
			}
		}
	}
}