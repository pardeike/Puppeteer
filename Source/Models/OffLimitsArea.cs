using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public class OffLimitsArea : IExposable, ILoadReferenceable, ICellBoolGiver
	{
		public int id;
		public string label;
		public Color color = Color.yellow;
		public Map map;
		public CellBoolDrawer drawer;
		public Texture2D texture;

		public BoolGrid innerGrid;
		public List<Restriction> restrictions;

		public int TrueCount => innerGrid.TrueCount;
		public Color Color => color;
		public IEnumerable<IntVec3> ActiveCells => innerGrid.ActiveCells;

		public OffLimitsArea()
		{
		}

		public OffLimitsArea(Map map, string useLabel = null)
		{
			this.map = map;
			id = Find.UniqueIDsManager.GetNextAreaID();
			innerGrid = new BoolGrid(map);
			restrictions = new List<Restriction>();
			color = Color.Lerp(new Color(Rand.Value, Rand.Value, Rand.Value), Color.gray, 0.25f);
			var offLimits = map.GetComponent<OffLimitsComponent>();
			if (useLabel != null)
			{
				label = useLabel;
				return;
			}
			for (var i = 1; true; i++)
			{
				label = "AreaDefaultLabel".Translate(i);
				if (offLimits.areas.Any(area => area.label == label) == false) break;
			}
		}

		public CellBoolDrawer Drawer
		{
			get
			{
				if (drawer == null)
					drawer = new StripesCellBoolDrawer(this, map);
				return drawer;
			}
		}

		public void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				if (restrictions == null)
					restrictions = new List<Restriction>();
				_ = restrictions.RemoveAll(res => res == null);
			}

			Scribe_Values.Look(ref id, "id", -1);
			Scribe_Values.Look(ref label, "label");
			Scribe_Values.Look(ref color, "color");
			Scribe_References.Look(ref map, "map");
			Scribe_Deep.Look(ref innerGrid, "innerGrid", Array.Empty<object>());
			Scribe_Collections.Look(ref restrictions, "restrictions", LookMode.Reference, Array.Empty<Restriction>());

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (restrictions == null)
					restrictions = new List<Restriction>();
				_ = restrictions.RemoveAll(res => res == null);
			}
		}

		public Texture2D ColorTexture
		{
			get
			{
				if (texture == null)
					texture = SolidColorMaterials.NewSolidColorTexture(color);
				return texture;
			}
		}

		public bool this[int index]
		{
			get => innerGrid[index];
			set => Set(map.cellIndices.IndexToCell(index), value);
		}

		public bool this[IntVec3 c]
		{
			get => innerGrid[map.cellIndices.CellToIndex(c)];
			set => Set(c, value);
		}

		public void Set(IntVec3 c, bool val)
		{
			var index = map.cellIndices.CellToIndex(c);
			if (innerGrid[index] != val)
			{
				innerGrid[index] = val;
				Drawer.SetDirty();
			}
		}

		public void Delete()
		{
			var offLimits = map.GetComponent<OffLimitsComponent>();
			_ = offLimits.areas.Remove(this);
		}

		public void MarkForDraw()
		{
			if (map == Find.CurrentMap)
				Drawer.MarkForDraw();
		}

		public void SetDirty()
		{
			Drawer.SetDirty();
		}

		public void Invert()
		{
			innerGrid.Invert();
			Drawer.SetDirty();
		}

		public bool GetCellBool(int index)
		{
			return innerGrid[index];
		}

		public Color GetCellExtraColor(int index)
		{
			if (color == Color.white) return new Color(1f, 1f, 1f, 0.99f);
			return color;
		}

		public string GetUniqueLoadID()
		{
			return string.Concat(new object[] { "OffLimits", id, "_", label });
		}

		public override string ToString()
		{
			return label;
		}
	}
}
