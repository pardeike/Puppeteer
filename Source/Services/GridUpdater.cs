using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class GridUpdater
	{
		public static readonly List<Color> colors = new List<Color>();
		public static readonly Dictionary<TerrainDef, int> terrains = new Dictionary<TerrainDef, int>();

		static GridUpdater()
		{
			DefDatabase<TerrainDef>.AllDefsListForReading.Do(def =>
			{
				if (terrains.ContainsKey(def) == false)
				{
					var color = ColorTools.GetMainColor(def.graphic);
					if (color.HasValue)
					{
						terrains[def] = colors.Count;
						colors.Add(color.Value);
						// Tools.SafeWarning($"#{terrains[def]} {def.defName} {color.Value.r} {color.Value.g} {color.Value.b}");
					}
				}
			});
		}

		public static string[] ColorList()
		{
			return colors
				.Select(color => string.Format(
					"#{0:x2}{1:x2}{2:x2}",
					(int)(color.r * 255),
					(int)(color.g * 255),
					(int)(color.b * 255))
				)
				.ToArray();
		}

		public static byte[] GetGrid(Pawn colonist, int x1, int y1, int x2, int y2)
		{
			bool HostileToColonist(Pawn pawn)
			{
				try
				{
					return pawn.HostileTo(colonist);
				}
				catch
				{
					return false;
				}
			}

			var width = x2 - x1 + 1;
			var height = y2 - y1 + 1;
			if (width <= 0 || height <= 0) return Array.Empty<byte>();
			var result = new byte[width * height * 2];

			var map = colonist.Map;
			var px = colonist.Position.x;
			var pz = colonist.Position.z;
			var thingGrid = map.thingGrid;
			var fogGrid = map.fogGrid;
			var terrainGrid = map.terrainGrid;
			var map_x = map.Size.x;
			var map_z = map.Size.z;
			var resultIndex = 0;
			var indices = map.cellIndices;
			var mapIndex = indices.CellToIndex(x1, y1);
			for (var z = y1; z <= y2; z++)
			{
				var ok = z >= 0 && z < map_z;
				var xi = 0;
				for (var x = x1; x <= x2; x++)
				{
					var byte1 = 255;
					var byte2 = 0;
					if (ok) ok = x >= 0 && x < map_x;
					if (ok)
					{
						var idx = mapIndex + xi;
						if (fogGrid.IsFogged(idx) == false)
						{
							_ = terrains.TryGetValue(terrainGrid.TerrainAt(idx), out byte1);

							var things = thingGrid.ThingsListAtFast(idx);
							Building building = null;
							Plant plant = null;
							var other = false;
							for (var i = 0; i < things.Count; i++)
							{
								var thing = things[i];
								if (thing is Building b)
									building = b;
								if (thing is Plant p)
									plant = p;
								else if (thing is Building_WorkTable_HeatPush)
									other = true;
							}
							if (building != null)
							{
								byte2 += 1;
								var passability = building.def.passability;
								if (passability == Traversability.PassThroughOnly)
									byte2 += 2;
								if (passability == Traversability.Standable)
									byte2 += 4;
							}
							if (plant != null) byte2 += 8;
							if (other) byte2 += 16;
						}
					}
					try
					{
						result[resultIndex++] = (byte)byte1;
						result[resultIndex++] = (byte)byte2;
					}
					catch (Exception e)
					{
						Log.Warning($"EX-B ({resultIndex}): {e}");
						throw;
					}
					xi++;
				}
				mapIndex += map_x;
			}
			foreach (var pawn in map.mapPawns.AllPawnsSpawned)
			{
				var x = pawn.Position.x;
				var z = pawn.Position.z;
				if (x >= x1 && x <= x2)
					if (z >= y1 && z <= y2)
					{
						var x0 = x - x1;
						var z0 = z - y1;
						var idx = z0 * width + x0;
						var byte2 = 32 + (pawn.IsColonist ? 64 : 0) + (HostileToColonist(pawn) ? 128 : 0);
						try
						{
							result[2 * idx + 1] += (byte)byte2;
						}
						catch (Exception e)
						{
							Log.Warning($"EX-A ({2 * idx + 1}): {e}");
							throw;
						}
					}
			}

			return result;
		}
	}
}