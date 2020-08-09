using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class Customizer
	{
		static List<HairDef> HairDefs => DefDatabase<HairDef>.AllDefs.Where(hair => hair.hairTags.SharesElementWith(FactionDefOf.PlayerColony.hairTags)).ToList();
		static List<BodyTypeDef> BodyDefs => DefDatabase<BodyTypeDef>.AllDefs.ToList();

		public static string[] AllHairStyle => HairDefs.Select(hair => GenText.CapitalizeAsTitle(hair.label)).OrderBy(s => s).ToArray();
		public static string[] AllBodyTypes => BodyDefs.Select(body => body.defName).OrderBy(s => s).ToArray();

		public static GameInfo.ColonistStyle GetStyle(Pawn pawn)
		{
			return new GameInfo.ColonistStyle()
			{
				gender = pawn.gender.ToString(),
				hairStyle = GenText.CapitalizeAsTitle(pawn.story.hairDef.label),
				bodyType = pawn.story.bodyType.defName,
				melanin = (int)(pawn.story.melanin * 100),
				hairColor = new[]
				{
					(int)(pawn.story.hairColor.r * 100),
					(int)(pawn.story.hairColor.g * 100),
					(int)(pawn.story.hairColor.b * 100)
				}
			};
		}

		static void RerenderPawn(Pawn pawn)
		{
			_ = State.pawnsToRefresh.Add(pawn);
		}

		public static void ChangeHairStyle(Pawn pawn, string label)
		{
			var style = HairDefs.FirstOrDefault(hair => hair.defName.ToLower() == label.ToLower());
			if (style == null) return;
			pawn.story.hairDef = style;
			RerenderPawn(pawn);
		}

		public static void ChangeHairColor(Pawn pawn, int r, int g, int b)
		{
			pawn.story.hairColor = new Color(0.01f * r, 0.01f * g, 0.01f * b);
			RerenderPawn(pawn);
		}

		public static void ChangeHairColor(Pawn pawn, int melanin)
		{
			pawn.story.melanin = Mathf.Max(0, Mathf.Min(1, 0.01f * melanin));
			RerenderPawn(pawn);
		}

		public static void ChangeGender(Pawn pawn, string gender)
		{
			if (gender.ToLower() == "female") pawn.gender = Gender.Female;
			else if (gender.ToLower() == "male") pawn.gender = Gender.Male;
			else return;
			RerenderPawn(pawn);
		}

		public static void ChangeBodyType(Pawn pawn, string label)
		{
			pawn.story.bodyType = DefDatabase<BodyTypeDef>.GetNamed(label);
			RerenderPawn(pawn);
		}

		internal static void Change(Pawn pawn, string key, string val)
		{
			switch (key)
			{
				case "gender":
					ChangeGender(pawn, val);
					break;
				case "melanin":
					if (int.TryParse(val, out var melanin))
						ChangeHairColor(pawn, melanin);
					break;
				case "bodyType":
					ChangeBodyType(pawn, val);
					break;
				case "hairStyle":
					ChangeHairStyle(pawn, val);
					break;
				case "hairColor":
					var m = new Regex(@"([0-9]+),([0-9]+),([0-9]+)").Match(val);
					if (m.Success)
					{
						var r = int.Parse(m.Groups[1].Value);
						var g = int.Parse(m.Groups[2].Value);
						var b = int.Parse(m.Groups[3].Value);
						ChangeHairColor(pawn, r, g, b);
					}
					break;
				default:
					Tools.LogWarning("Unknown command {key}");
					break;
			}
		}
	}
}