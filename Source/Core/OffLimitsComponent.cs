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
		[HarmonyPriority(Priority.First)]
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
		[HarmonyPriority(Priority.First)]
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

	public class PawnSettings : IExposable
	{
		public bool enabled = true;
		public HashSet<OffLimitsArea> activeAreas = new HashSet<OffLimitsArea>();

		public PawnSettings()
		{
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref enabled, "enabled");
			Scribe_Collections.Look(ref activeAreas, "activeAreas", LookMode.Reference);
		}

		public static PawnSettings SettingsFor(Pawn pawn)
		{
			var map = Find.CurrentMap;
			if (map == null) return new PawnSettings();
			var pawnSettings = map.GetComponent<OffLimitsComponent>().pawnSettings;
			if (pawnSettings.TryGetValue(pawn, out var settings) == false)
			{
				settings = new PawnSettings();
				pawnSettings[pawn] = settings;
			}
			return settings;
		}
	}

	public class OffLimitsComponent : MapComponent
	{
		public List<OffLimitsArea> areas = new List<OffLimitsArea>();
		public List<Restriction> restrictions = new List<Restriction>();
		public Dictionary<Pawn, PawnSettings> pawnSettings = new Dictionary<Pawn, PawnSettings>();

		public OffLimitsComponent(Map map) : base(map) { }

		static readonly Color transparentBlack = new Color(0, 0, 0, 0.15f);

		List<Pawn> tmpKeys;
		List<PawnSettings> tmpVals;
		public override void ExposeData()
		{
			base.ExposeData();

			Scribe_Collections.Look(ref areas, "areas", LookMode.Deep, Array.Empty<OffLimitsArea>());
			Scribe_Collections.Look(ref restrictions, "restrictions", LookMode.Deep, Array.Empty<Restriction>());

			if (areas == null)
				areas = new List<OffLimitsArea>();
			if (restrictions == null)
				restrictions = new List<Restriction>();

			if (Scribe.mode == LoadSaveMode.Saving)
			{
				tmpKeys = new List<Pawn>(pawnSettings.Keys);
				tmpVals = new List<PawnSettings>(pawnSettings.Values);
			}

			Scribe_Collections.Look(ref tmpKeys, "pawnSettings.pawns", LookMode.Reference);
			Scribe_Collections.Look(ref tmpVals, "pawnSettings.settings", LookMode.Deep);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				pawnSettings = new Dictionary<Pawn, PawnSettings>();
				for (var i = 0; i < tmpKeys.Count; i++)
					pawnSettings[tmpKeys[i]] = tmpVals[i];
			}
		}

		public OffLimitsArea GetLabeled(string label)
		{
			return areas.FirstOrDefault(area => area.label == label);
		}

		public override void MapComponentOnGUI()
		{
			base.MapComponentOnGUI();

			var selectedArea = Tools.GetSelectedArea();
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