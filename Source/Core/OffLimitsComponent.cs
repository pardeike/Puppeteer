using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace Puppeteer
{
	[HarmonyPatch(typeof(CellBoolDrawer), "ActuallyDraw")]
	class CellBoolDrawer_ActuallyDraw_Patch
	{
		public static bool Prefix(CellBoolDrawer __instance)
		{
			if (__instance is StripesCellBoolDrawer)
				return Puppeteer.Settings.showOffLimitZones;
			return true;
		}
	}

	[HarmonyPatch(typeof(CellBoolDrawer), "FinalizeWorkingDataIntoMesh")]
	class CellBoolDrawer_FinalizeWorkingDataIntoMesh_Patch
	{
		public static void Postfix(CellBoolDrawer __instance, Mesh mesh)
		{
			if ((__instance is StripesCellBoolDrawer) == false) return;

			var uvs = new Vector2[mesh.vertices.Length];
			if (mesh.vertices.Length > 0)
			{
				var xs = mesh.vertices.Select(v => v.x);
				var zs = mesh.vertices.Select(v => v.z);
				var mx = (xs.Min() + xs.Max()) / 2;
				var mz = (zs.Min() + zs.Max()) / 2;

				for (var i = 0; i < uvs.Length; i++)
				{
					var v = mesh.vertices[i];
					uvs[i] = new Vector2(v.x - mx, v.z - mz);
				}
			}
			mesh.uv = uvs;
		}
	}

	public class OffLimitsComponent : MapComponent
	{
		public List<OffLimitsArea> areas = new List<OffLimitsArea>();
		public List<Restriction> restrictions = new List<Restriction>();

		public OffLimitsComponent(Map map) : base(map) { }

		static readonly Color transparentBlack = new Color(0, 0, 0, 0.15f);

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref areas, "areas", LookMode.Deep, Array.Empty<OffLimitsArea>());
			Scribe_Collections.Look(ref restrictions, "restrictions", LookMode.Deep, Array.Empty<Restriction>());
		}

		public OffLimitsArea GetLabeled(string label)
		{
			return areas.FirstOrDefault(area => area.label == label);
		}

		public override void MapComponentOnGUI()
		{
			base.MapComponentOnGUI();

			var selectedArea = (Find.DesignatorManager.SelectedDesignator as Designator_OffLimits)?.area;
			areas.DoIf(area => area.drawer != null, area =>
			{
				var material = StripesCellBoolDrawer.materialRef(area.drawer);

				var c1 = area.color;
				c1.a = 0.25f;
				var c2 = transparentBlack;
				if (selectedArea == area)
				{
					c1.a = 0.5f;
					c2.a = 0.5f;
				}
				material.SetColor("_Color1", c1);
				material.SetColor("_Color2", c2);
			});
		}
	}

	public class StripesCellBoolDrawer : CellBoolDrawer
	{
		public static readonly FieldRef<CellBoolDrawer, Material> materialRef = FieldRefAccess<CellBoolDrawer, Material>("material");

		public StripesCellBoolDrawer(ICellBoolGiver giver, Map map) : base(giver, map.Size.x, map.Size.z, 3699)
		{
			var material = new Material(Assets.StripesMaterial);
			material.SetColor("_Color1", Color.clear);
			material.SetColor("_Color2", Color.clear);
			materialRef(this) = material;
		}
	}
}