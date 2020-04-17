using System;
using System.Diagnostics;
using System.Timers;
using UnityEngine;
using Verse;
using WebSocketSharp;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class Puppet
	{
		const float puppetOutIncrement = 1 / 60f * 2f;
		const int bPadding = 10;
		const int bRightMargin = 19;
		const int bBottomMargin = 11;

		static readonly Texture2D puppetTex = Assets.puppet;
		static readonly Texture2D bubbleTex = Assets.bubble;

		static float puppetOutDesired = 0;
		static float puppetOut = 0;
		static Timer puppetOutTimer = null;

		static string text = "";
		public static bool IsShowing => puppetOut > 0;

		public static void Say(string message, int? secs = null)
		{
			if (message.IsNullOrEmpty()) return;
			lock (text)
			{
				text = message;
				puppetOutDesired = 1;
				var seconds = 2 + message.Length / 80;
				var s = secs ?? seconds;
				if (puppetOutTimer != null)
				{
					puppetOutTimer.Stop();
					puppetOutTimer = null;
				}
				puppetOutTimer = new Timer(s * 1000);
				puppetOutTimer.Elapsed += (sender, e) => puppetOutDesired = 0;
				puppetOutTimer.Start();
			}
		}

		public static void Update()
		{
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
				var oldFont = Text.Font;
				var oldColor = GUI.color;

				Text.Font = GameFont.Small;
				for (var n = 0; n < 10; n++)
				{
					var bWidth = 120 + 40 * n;
					var maxWidth = bWidth - 2 * bPadding - bRightMargin;
					var textHeight = Text.CalcHeight(text, maxWidth);
					var bHeight = textHeight + 2 * bPadding + bBottomMargin;
					if (textHeight < 25 || bHeight < bWidth / 3)
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

				Text.Font = oldFont;
				GUI.color = oldColor;
			}
		}
	}
}
