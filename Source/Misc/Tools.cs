using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace Puppeteer
{
	public static class Tools
	{
		public static string Base64Decode(this string value)
		{
			value = value.Replace('-', '+');
			value = value.Replace('_', '/');

			value = value.PadRight(value.Length + (4 - value.Length % 4) % 4, '=');

			var data = Convert.FromBase64String(value);
			return Encoding.UTF8.GetString(data);
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

		public static void RenderColonists(Map map, bool isVisibleMap)
		{
			var colonists = new HashSet<Pawn>(map.mapPawns.FreeColonists);
			Renderer.fakeZoom = true;

			if (isVisibleMap)
			{
				var viewRect = Find.CameraDriver.CurrentViewRect.ContractedBy(2);
				var visibleColonists = colonists.Where(c => viewRect.Contains(c.Position)).ToList();
				visibleColonists.Do(c =>
				{
					Renderer.GetPawnScreenRender(c, 1.5f);
					_ = colonists.Remove(c);
				});
				if (colonists.Count == 0) return;
			}
			else
			{
				map.weatherManager.DrawAllWeather();
			}

			var rememberedMap = Find.CurrentMap;
			SetCurrentMapDirectly(map);

			Renderer.renderOffset = CurrentMapOffset();
			Renderer.fakeViewRect = new CellRect(0, 0, map.Size.x, map.Size.z);

			colonists.Do(c => map.glowGrid.MarkGlowGridDirty(c.Position));

			map.skyManager.SkyManagerUpdate();
			map.powerNetManager.UpdatePowerNetsAndConnections_First();
			map.glowGrid.GlowGridUpdate_First();

			PlantFallColors.SetFallShaderGlobals(map);
			//map.waterInfo.SetTextures();

			colonists.Do(c =>
			{
				var pos = c.Position;
				Renderer.fakeViewRect = new CellRect(pos.x - 3, pos.z - 3, pos.x + 3, pos.z + 3);

				map.mapDrawer.MapMeshDrawerUpdate_First();
				map.mapDrawer.DrawMapMesh();
				map.dynamicDrawManager.DrawDynamicThings();
			});

			map.gameConditionManager.GameConditionManagerDraw(map);
			MapEdgeClipDrawer.DrawClippers(map);
			map.designationManager.DrawDesignations();
			map.overlayDrawer.DrawAllOverlays();

			colonists.Do(c => Renderer.GetPawnScreenRender(c, 1.5f));

			Renderer.fakeViewRect = CellRect.Empty;
			SetCurrentMapDirectly(rememberedMap);
			Renderer.renderOffset = 0f;
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
			var dm = new DynamicMethod(s_name, typeof(T), new Type[0], type, true);

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