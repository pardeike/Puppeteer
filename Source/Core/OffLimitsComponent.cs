using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	[HarmonyPatch(typeof(CellBoolDrawer), "ActuallyDraw")]
	class CellBoolDrawer_ActuallyDraw_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static bool Prefix(CellBoolDrawer __instance)
		{
			if (__instance is StripesCellBoolDrawer)
				return PuppeteerMod.Settings.showOffLimitZones || Find.DesignatorManager.SelectedDesignator is Designator_OffLimits;
			return true;
		}
	}

	[HarmonyPatch(typeof(CellBoolDrawer), "CreateMaterialIfNeeded")]
	class CellBoolDrawer_CreateMaterialIfNeeded_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static bool Prefix(CellBoolDrawer __instance, ref Material ___material, Func<Color> ___colorGetter, ref bool ___materialCaresAboutVertexColors)
		{
			if ((__instance is StripesCellBoolDrawer) == false) return true;

			var color = ___colorGetter();
			___material = SolidColorMaterials.SimpleSolidColorMaterial(new Color(color.r, color.g, color.b, 0.4f), false);
			___materialCaresAboutVertexColors = true;
			___material.renderQueue = 3699;
			return false;
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
			if (pawn?.Map == null) return new PawnSettings();
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

		List<Pawn> tmpKeys = new List<Pawn>();
		List<PawnSettings> tmpVals = new List<PawnSettings>();
		public override void ExposeData()
		{
			try
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
			catch
			{
				if (Scribe.mode == LoadSaveMode.PostLoadInit)
				{
					Log.Warning("Could not load Puppeteer areas and restrictions. They were reset to their defaults");
					pawnSettings = new Dictionary<Pawn, PawnSettings>();
				}
			}
		}

		public OffLimitsArea GetLabeled(string label)
		{
			return areas.FirstOrDefault(area => area.label == label);
		}
	}

	public class StripesCellBoolDrawer : CellBoolDrawer
	{
		public StripesCellBoolDrawer(ICellBoolGiver giver, Map map) : base(giver, map.Size.x, map.Size.z) { }
	}
}
