using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public class VersionDialog : Window
	{
		public override Vector2 InitialSize => new Vector2(476, 640);

		protected float contentWidth;
		protected float contentHeight;
		protected Vector2 scrollPosition;

		readonly List<VersionInformation.Version> versions;
		readonly Action onClose;

		public VersionDialog(List<VersionInformation.Version> versions, Action onClose)
		{
			this.versions = versions;
			this.onClose = onClose;

			absorbInputAroundWindow = true;
			doCloseX = true;
			doCloseButton = true;
			absorbInputAroundWindow = true;
			preventDrawTutor = true;
			scrollPosition = new Vector2(0f, 0f);
		}

		public override void Close(bool doCloseSound = true)
		{
			onClose?.Invoke();
			base.Close(doCloseSound);
		}

		public override void PostClose()
		{
			onClose?.Invoke();
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Medium;
			GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
			Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "NewPuppeteerVersion".Translate());
			GenUI.ResetLabelAlign();

			if (contentHeight == 0)
			{
				var l = new Listing_Standard() { maxOneColumn = true };
				contentWidth = inRect.width - 20;
				l.Begin(new Rect(0, 0, contentWidth, 0));
				contentHeight = DoContent(l);
				l.End();
			}

			var outerRect = new Rect(inRect.x, inRect.y + 60, inRect.width, inRect.height - 2 * 60);
			var innerRect = new Rect(0f, 0f, contentWidth, contentHeight);
			Widgets.BeginScrollView(outerRect, ref scrollPosition, innerRect, true);
			var list = new Listing_Standard() { maxOneColumn = true };
			list.Begin(innerRect);
			_ = DoContent(list);
			list.End();
			Widgets.EndScrollView();
		}

		float DoContent(Listing_Standard list)
		{
			// width for images is 420

			var width = list.ColumnWidth;
			foreach (var version in versions)
			{
				Header(list, $"Version {version.version}");
				foreach (var info in version.infos)
				{
					TextBlock(list, info.title, 4f, true);
					Image(list, info.image);
					for (var i = 0; i < info.texts.Length; i++)
						TextBlock(list, info.texts[i], i < info.texts.Length - 1 ? 0f : 4f);
				}
				list.Gap(20f);
			}
			return list.CurHeight;
		}

		void Header(Listing_Standard list, string title)
		{
			var inset = 4f;
			var textHeight = Text.CalcHeight(title, list.ColumnWidth - 2 * inset);
			var rect = new Rect(0, list.CurHeight, list.ColumnWidth, textHeight).Rounded();
			GUI.DrawTexture(rect, BaseContent.WhiteTex);
			GUI.color = new Color(21f / 255f, 25f / 255f, 29f / 255f);
			rect.xMin += inset;
			rect.xMax -= inset;
			Widgets.Label(rect, title);
			GUI.color = Color.white;
			list.Gap(textHeight + 2 * inset);
		}

		void TextBlock(Listing_Standard list, string text, float padding = 4f, bool bigger = false)
		{
			if (text.NullOrEmpty()) return;
			var inset = 0f;
			if (text.StartsWith("- ")) { inset = 10f; text = text.Substring(2); }
			Text.Font = bigger ? GameFont.Medium : GameFont.Small;
			var textHeight = Text.CalcHeight(text, list.ColumnWidth - inset);
			var rect = list.GetRect(textHeight).Rounded();
			if (inset > 0f) Widgets.Label(rect, "-");
			rect.x += inset;
			Widgets.Label(rect, text);
			list.Gap(padding);
		}

		void Image(Listing_Standard list, string name, float padding = 4f)
		{
			if (name.NullOrEmpty()) return;
			var tex = ContentFinder<Texture2D>.Get($"Versions/{name}", false);
			if (tex == null) return;
			var height = list.ColumnWidth * tex.height / tex.width;
			var rect = new Rect(0, list.CurHeight, list.ColumnWidth, height);
			GUI.DrawTexture(rect, tex);
			list.Gap(height + padding);
		}

		/*void Divider(Listing_Standard list, float prePadd = 20f, float postPadd = 3f)
		{
			list.Gap(prePadd);
			GUI.color = Color.gray;
			Widgets.DrawLineHorizontal(0f, list.CurHeight, list.ColumnWidth);
			GUI.color = Color.white;
			list.Gap(postPadd);
		}*/
	}

	public static class VersionInformation
	{
		const string versionFileName = "VersionInfo.json";
		const string lastSeenFileName = "PuppeteerLastSeenVersion.json";

		public static string rootDir;

		public class Info
		{
			public string title;
			public string image;
			public string[] texts;
		}

		public class Version
		{
			public string version;
			public Info[] infos;
		}

		public static void Show()
		{
			var path = $"{rootDir}{Path.DirectorySeparatorChar}About{Path.DirectorySeparatorChar}{versionFileName}";
			if (File.Exists(path) == false) return;
			var data = File.ReadAllText(path, Encoding.UTF8);
			if (data == null) return;
			var allVersions = JsonConvert.DeserializeObject<Version[]>(data).ToList();

			var lastSeen = lastSeenFileName.ReadConfig();
			var idx = allVersions.FindIndex(v => v.version == lastSeen);
			if (idx >= 0) allVersions.RemoveRange(0, idx + 1);

			var currentVersion = ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(
				Assembly.GetAssembly(typeof(VersionInformation)),
				typeof(AssemblyFileVersionAttribute), false)
			).Version;

			if (allVersions.Any())
				Find.WindowStack.Add(new VersionDialog(allVersions, () => lastSeenFileName.WriteConfig(currentVersion)));
		}
	}
}