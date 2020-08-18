using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Internal;
using Verse;
using static HarmonyLib.AccessTools;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class TwitchToolkit
	{
		static readonly Action<object, OnMessageReceivedArgs> OnMessageReceived = Tools.GetOptionalMethod<Action<object, OnMessageReceivedArgs>>("ToolkitCore.TwitchWrapper", "OnMessageReceived");
		static readonly Func<string, object> GetViewer = Tools.GetOptionalMethod<Func<string, object>>("TwitchToolkit.Viewers", "GetViewer");
		static readonly MethodInfo m_GetViewerCoins = Method("TwitchToolkit.Viewer:GetViewerCoins");
		static readonly FieldRef<bool> UnlimitedCoins = Tools.GetOptionalStaticFieldRef<bool>("TwitchToolkit.ToolkitSettings", "UnlimitedCoins");
		static readonly Action<int> AwardViewersCoins = Tools.GetOptionalMethod<Action<int>>("TwitchToolkit.Viewers", "AwardViewersCoins");

		public static bool Exists => true
			&& OnMessageReceived != null
			&& GetViewer != null
			&& m_GetViewerCoins != null
			&& UnlimitedCoins != null;

		public static void RefreshViewers()
		{
			AwardViewersCoins?.Invoke(0);
		}

		public static int GetCurrentCoins(string userName)
		{
			if (Exists == false) return -1;
			var userNameLowerCase = userName.ToLower();
			var viewer = GetViewer(userNameLowerCase);
			if (viewer == null) return -2;
			if (UnlimitedCoins()) return 99999999;
			return (int)m_GetViewerCoins.Invoke(viewer, new object[0]);
		}

		public static void SendMessage(string userId, string userName, string message)
		{
			// Tools.LogWarning($"USER {userName} SEND {message}");
			var userNameLowerCase = userName.ToLower();

			if (Exists == false) return;

			var tags = new Dictionary<string, string>
			{
				["user-id"] = userId,
				["user-type"] = "viewer",
				["color"] = "#FFFFFF",
			};
			if (message.StartsWith("!") == false) message = $"!{message}";
			var ircMessage = new IrcMessage(TwitchLib.Client.Enums.Internal.IrcCommand.Unknown, new string[] { "", message }, userNameLowerCase, tags);
			var channelEmotes = new MessageEmoteCollection();
			var chatMessage = new ChatMessage("Puppeteer", ircMessage, ref channelEmotes, false);

			var messageArgs = new OnMessageReceivedArgs() { ChatMessage = chatMessage };
			OnMessageReceived(null, messageArgs);
		}

		[HarmonyPatch]
		static class TwitchWrapper_SendChatMessage_Patch
		{
			static readonly MethodBase method = Method("ToolkitCore.TwitchWrapper:SendChatMessage");
			static readonly Regex parser = new Regex("(.*)\\@([^ ]+)(.*)");
			static readonly Regex byRemover = new Regex(" by$");

			public static bool Prepare()
			{
				return method != null;
			}

			public static MethodBase TargetMethod()
			{
				return method;
			}

			public static bool Prefix(string message)
			{
				// Tools.LogWarning($"CHAT RECEIVED {message}");

				message = Tools.PureAscii(message
					.Replace("💰", "Coins")
					.Replace("⚖", "Karma")
					.Replace("📈", "Rate")
					.Replace("⎮", "|"));

				var match = parser.Match(message);
				if (match.Success == false) return true;

				var userName = match.Groups[2].Value;

				var msg1 = match.Groups[1].Value.Trim();
				msg1 = byRemover.Replace(msg1, "");

				var mgs2 = match.Groups[3].Value.Trim();
				var userMessage = $"{msg1} {mgs2}".Trim();

				var puppeteer = State.Instance.PuppeteerForViewerName(userName);
				if (puppeteer == null || puppeteer.IsConnected == false) return true;

				Controller.instance.SendChatMessage(puppeteer.vID, userMessage);
				return false;
			}
		}
	}
}