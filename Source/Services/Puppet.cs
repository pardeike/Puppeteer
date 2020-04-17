using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class Puppet
	{
		const float puppetOutIncrement = 1 / 60f;
		const int bPadding = 10;
		const int bRightMargin = 19;
		const int bBottomMargin = 11;

		static readonly Texture2D puppetTex = Assets.puppet;
		static readonly Texture2D bubbleTex = Assets.bubble;

		static float puppetOutDesired = 0;
		static float puppetOut = 0;
		static Timer puppetOutTimer = null;

		static string text = "";

		public static void Say(string message, int? secs = null)
		{
			text = message;
			puppetOutDesired = 1;
			var seconds = 2 + message.Length / 70;
			var s = secs ?? seconds;
			if (puppetOutTimer != null)
				_ = puppetOutTimer.Change(s * 1000, Timeout.Infinite);
			else
				puppetOutTimer = new Timer((_) => puppetOutDesired = 0, null, s * 1000, Timeout.Infinite);
		}

		public static void Update()
		{
			/* TODO: for testing
			if (Widgets.ButtonText(new Rect(100, 100, 80, 20), "Connect"))
				Say("Connecting");
			if (Widgets.ButtonText(new Rect(100, 130, 80, 20), "Little"))
				Say("This mod is powered by Harmony - your favorite moddling library");
			if (Widgets.ButtonText(new Rect(100, 160, 80, 20), "Yay!"))
				Say("Yay!");
			if (Widgets.ButtonText(new Rect(100, 190, 80, 20), "Much"))
				Say("Maybe you're doing it the wrong way. You're getting the width of the text because the width doesn't change when the text overflows, but there's a way to make it change according to the text width (so the overflow configuration is not needed). Add a Content Size Fitter component to the same game object with the Text component, and set Horizontal Fit to Preferred size. The gameObject's size will change according to the pivot, set pivot.x to 0.5 and it will be centered.");*/

			puppetOut += puppetOutIncrement * Math.Sign(puppetOutDesired - puppetOut);
			if (puppetOut < 0) puppetOut = 0;
			if (puppetOut > 1) puppetOut = 1;

			var xPos = UI.screenWidth - (int)(puppetTex.width * Math.Sin(Math.PI / 2 * puppetOut));
			var yPos = 80;
			var width = puppetTex.width;
			var height = puppetTex.height;
			var rect = new Rect(xPos, yPos, puppetTex.width, puppetTex.height);
			if (Widgets.ButtonImage(rect, puppetTex))
			{
				_ = Process.Start("https://puppeteer.rimworld.live");
			}

			if (puppetOut >= 0.8f)
			{
				Text.Font = GameFont.Small;
				for (var n = 0; n < 20; n++)
				{
					var bWidth = 160 + 40 * n;
					var maxWidth = bWidth - 2 * bPadding - bRightMargin;
					var textHeight = Text.CalcHeight(text, maxWidth);
					var bHeight = textHeight + 2 * bPadding + bBottomMargin;
					if (bHeight < bWidth / 3)
					{
						var xMax = xPos + 35;
						var zMax = yPos + height - 24;
						var xMin = xMax - bWidth;
						var zMin = zMax - bHeight;
						rect = new Rect(xMin, zMin, xMax - xMin, zMax - zMin);
						GUI.color = new Color(1, 1, 1, (puppetOut - 0.8f) * 5f);
						Widgets.DrawAtlas(rect, bubbleTex);
						GUI.color = Color.white;

						if (puppetOut == 1f)
						{
							var d = textHeight < 25 ? 3 : 0;
							var textRect = new Rect(rect.x + bPadding + d, rect.y + bPadding + d, maxWidth, textHeight);
							GUI.color = Color.black;
							Widgets.Label(textRect, text);
						}

						break;
					}
				}
			}
		}
	}
}
