using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class Tools
	{
		static Tools()
		{
			RoundRobbin.Create("update-colonist", 5f);
			RoundRobbin.Create("render-colonist", 30f);
		}

		public static bool IsLocalDev()
		{
			var path = Path.Combine(GenFilePaths.ConfigFolderPath, "PuppeteerLocalDevelopment.txt");
			return File.Exists(path);
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

		public static int[] GetRGB(Color color)
		{
			return new[] { (int)(255 * color.r), (int)(255 * color.g), (int)(255 * color.b) };
		}

		public static string AsString(this DateTime dateTime)
		{
			return dateTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
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
			return pawn.Spawned == false
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
				.FirstOrDefault(carrier => carrier.carryTracker.CarriedThing.thingIDNumber == thingID);
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

		public static readonly Dictionary<TimeAssignmentDef, string> Assignments = new Dictionary<TimeAssignmentDef, string>()
		{
			{ TimeAssignmentDefOf.Anything, "A" },
			{ TimeAssignmentDefOf.Work, "W" },
			{ TimeAssignmentDefOf.Joy, "J" },
			{ TimeAssignmentDefOf.Sleep, "S" },
		};

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
			var pos = pawn.Position;
			var traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassDoors, false);
			var path = pawn.Map.pathFinder.FindPath(pawn.Position, destination, traverseParams, PathEndMode.Touch);
			if (path == PawnPath.NotFound) return 999999;
			var cost = path.TotalCost;
			var min = Math.Floor(cost * 60f / GenDate.TicksPerHour);
			path.ReleaseToPool();
			return min;
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
			if (pawn == null) return;
			var name3 = pawn.Name as NameTriple;
			if (name3 != null)
				pawn.Name = new NameTriple(name3.First, nick ?? name3.First, name3.Last);
		}

		public static void UpdateColonists(bool updateAll)
		{
			if (updateAll)
			{
				Current.Game.Maps.SelectMany(map => PlayerPawns.FreeColonists(map, false))
					.Do(p => Puppeteer.instance.UpdateColonist(p));
				return;
			}

			var pawn = RoundRobbin.NextColonist("update-colonist");
			if (pawn != null)
				Puppeteer.instance.UpdateColonist(pawn);
		}

		public static void RenderColonists()
		{
			var pawn = RoundRobbin.NextColonist("render-colonist");
			var colonist = Puppeteer.instance.colonists.FindColonist(pawn);

			pawn = GetCarrier(pawn) ?? pawn;

			if (pawn == null) return;
			var map = pawn.Map;
			if (map == null) return;
			var currentMap = Find.CurrentMap;
			var isVisibleMap = map == currentMap && WorldRendererUtility.WorldRenderedNow == false;

			if (isVisibleMap)
			{
				var viewRect = Find.CameraDriver.CurrentViewRect.ContractedBy(2);
				var visible = viewRect.Contains(pawn.Position);
				if (visible)
				{
					Renderer.PawnScreenRender(colonist, pawn.DrawPos, 1.5f);
					return;
				}
			}
			else
				map.weatherManager.DrawAllWeather();

			Renderer.fakeZoom = true;
			SetCurrentMapDirectly(map);
			Renderer.renderOffset = CurrentMapOffset();
			Renderer.fakeViewRect = new CellRect(0, 0, map.Size.x, map.Size.z);

			map.glowGrid.MarkGlowGridDirty(pawn.Position);

			map.skyManager.SkyManagerUpdate();
			map.powerNetManager.UpdatePowerNetsAndConnections_First();
			map.glowGrid.GlowGridUpdate_First();

			PlantFallColors.SetFallShaderGlobals(map);
			//map.waterInfo.SetTextures();

			var pos = pawn.Position;
			Renderer.fakeViewRect = new CellRect(pos.x - 3, pos.z - 3, pos.x + 3, pos.z + 3);

			map.mapDrawer.MapMeshDrawerUpdate_First();
			map.mapDrawer.DrawMapMesh();
			map.dynamicDrawManager.DrawDynamicThings();

			map.gameConditionManager.GameConditionManagerDraw(map);
			MapEdgeClipDrawer.DrawClippers(map);
			map.designationManager.DrawDesignations();
			map.overlayDrawer.DrawAllOverlays();

			Renderer.PawnScreenRender(colonist, pawn.DrawPos, 1.5f);

			Renderer.fakeViewRect = CellRect.Empty;
			Renderer.renderOffset = 0f;
			SetCurrentMapDirectly(currentMap);
			Renderer.fakeZoom = false;
		}

		public delegate ref T StaticFieldRef<T>();
		public static StaticFieldRef<T> StaticFieldRefAccess<T>(Type type, string name)
		{
			var fieldInfo = AccessTools.Field(type, name);
			if (fieldInfo == null)
				throw new ArgumentNullException(nameof(fieldInfo));
			if (!typeof(T).IsAssignableFrom(fieldInfo.FieldType))
				throw new ArgumentException("FieldInfo type does not match FieldRefAccess return type.");
			if (typeof(T) != typeof(object))
				if (fieldInfo.DeclaringType == null || !fieldInfo.DeclaringType.IsAssignableFrom(type))
					throw new MissingFieldException(type.Name, fieldInfo.Name);

			var s_name = "__refget_" + type.Name + "_fi_" + fieldInfo.Name;

			// workaround for using ref-return with DynamicMethod:
			// a.) initialize with dummy return value
			var dm = new DynamicMethod(s_name, typeof(T), Array.Empty<Type>(), type, true);

			// b.) replace with desired 'ByRef' return value
			var trv = Traverse.Create(dm);
			_ = trv.Field("returnType").SetValue(typeof(T).MakeByRefType());
			_ = trv.Field("m_returnType").SetValue(typeof(T).MakeByRefType());

			var il = dm.GetILGenerator();
			il.Emit(OpCodes.Ldsflda, fieldInfo);
			il.Emit(OpCodes.Ret);
			return (StaticFieldRef<T>)dm.CreateDelegate(typeof(StaticFieldRef<T>));
		}
	}
}