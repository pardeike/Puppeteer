using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Client.Models.Internal;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class TwitchToolkit
	{
		static readonly Action<object, OnMessageReceivedArgs> OnMessageReceived;

		static TwitchToolkit()
		{
			var t_TwitchWrapper = AccessTools.TypeByName("ToolkitCore.TwitchWrapper");
			var m_OnMessageReceived = AccessTools.Method(t_TwitchWrapper, "OnMessageReceived");
			if (m_OnMessageReceived != null)
				OnMessageReceived = AccessTools.MethodDelegate<Action<object, OnMessageReceivedArgs>>(m_OnMessageReceived);
		}

		public static bool Exists => OnMessageReceived != null;

		public static void SendMessage(string userId, string userName, string message)
		{
			if (OnMessageReceived == null) return;

			var tags = new Dictionary<string, string>
			{
				["user-id"] = userId,
				["user-type"] = "viewer"
			};
			var ircMessage = new IrcMessage(TwitchLib.Client.Enums.Internal.IrcCommand.Unknown, new string[] { "", message }, userName, tags);
			var channelEmotes = new MessageEmoteCollection();
			var chatMessage = new ChatMessage("Puppeteer", ircMessage, ref channelEmotes, false);

			var messageArgs = new OnMessageReceivedArgs() { ChatMessage = chatMessage };
			OnMessageReceived(null, messageArgs);
		}

		[HarmonyPatch]
		static class TwitchWrapper_SendChatMessage_Patch
		{
			public static bool Prepare(MethodBase original)
			{
				if (original == null) return true;
				return TargetMethod() != null;
			}

			public static MethodBase TargetMethod()
			{
				return AccessTools.Method("ToolkitCore.TwitchWrapper:SendChatMessage");
			}

			public static bool Prefix(string message)
			{
				var parts = message.Split('→').ToList();
				var userName = parts[0].Trim();
				if (userName.StartsWith("@")) userName = userName.Substring(1);
				var puppeteer = State.Instance.PuppeteerForViewerName(userName);
				if (puppeteer == null || puppeteer.connected == false) return true;
				//parts.RemoveAt(0);
				//var text = parts.Join(null, "→");
				Controller.instance.SendChatMessage(puppeteer.vID, /*text*/ message);
				return false;
			}
		}
	}
}