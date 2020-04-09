using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class Tools
	{
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
				.SelectMany(map => map.mapPawns.FreeColonists)
				.FirstOrDefault(pawn => pawn.thingIDNumber == thingID);
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

		static int colonistTicks = 0;
		const float colonistEveryTicks = 60f;
		static int colonistCounter = -1;
		static Pawn ColonistRoundRobbin()
		{
			var colonists = Current.Game.Maps.SelectMany(map => map.mapPawns.FreeColonists).ToList();
			if (colonists.Count == 0) return null;
			var delay = colonistEveryTicks / colonists.Count + 1;
			colonistTicks++;
			if (colonistTicks < delay) return null;
			colonistTicks = 0;
			var idx = (colonistCounter + 1) % colonists.Count;
			colonistCounter = idx;
			return colonists[idx];
		}

		public static void SetColonistNickname(Pawn pawn, string nick)
		{
			if (pawn == null) return;
			var name3 = pawn.Name as NameTriple;
			if (name3 != null)
				pawn.Name = new NameTriple(name3.First, nick ?? name3.First, name3.Last);
		}

		public static void RenderColonists()
		{
			var pawn = ColonistRoundRobbin();
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
					Renderer.PawnScreenRender(pawn, 1.5f);
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

			Renderer.PawnScreenRender(pawn, 1.5f);

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