using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	class Main
	{
		static Main()
		{
			var harmony = new Harmony("net.pardeike.harmony.Puppeteer");
			harmony.PatchAll();
		}
	}

	public class Puppeteer : Mod
	{
		const string settingsFileName = "PuppeteerSettings.json";
		public static Settings Settings = new Settings();

		public Puppeteer(ModContentPack content) : base(content)
		{
			VersionInformation.rootDir = content.RootDir;
			LoadSettings();
		}

		public override string SettingsCategory()
		{
			return "Puppeteer";
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			SettingsDrawer.DoWindowContents(ref Puppeteer.Settings, inRect);
		}

		public override void WriteSettings()
		{
			base.WriteSettings();
			SaveSettings();
		}

		public static void LoadSettings()
		{
			var data = settingsFileName.ReadConfig();
			Settings = data == null ? new Settings() : JsonConvert.DeserializeObject<Settings>(data);
		}

		public static void SaveSettings()
		{
			var data = JsonConvert.SerializeObject(Settings, Formatting.Indented);
			settingsFileName.WriteConfig(data);
		}

		public static void ReseSettings()
		{
			Settings = new Settings();
			SettingsDrawer.scrollPosition = Vector2.zero;
		}
	}
}