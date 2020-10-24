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

	public class PuppeteerMod : Mod
	{
		const string settingsFileName = "PuppeteerSettings.json";
		public static Settings Settings = new Settings();

		public PuppeteerMod(ModContentPack content) : base(content)
		{
			LoadSettings();
		}

		public override string SettingsCategory()
		{
			return "Puppeteer";
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			SettingsDrawer.DoWindowContents(ref PuppeteerMod.Settings, inRect);
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
			var data = JsonConvert.SerializeObject(Settings, Formatting.None);
			settingsFileName.WriteConfig(data);
		}

		public static void ReseSettings()
		{
			Settings = new Settings();
			SettingsDrawer.scrollPosition = Vector2.zero;
		}
	}
}