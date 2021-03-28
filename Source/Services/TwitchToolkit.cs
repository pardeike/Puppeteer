using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
		static readonly Func<List<string>> ParseViewersFromJsonAndFindActiveViewers = Tools.GetOptionalMethod<Func<List<string>>>("TwitchToolkit.Viewers", "ParseViewersFromJsonAndFindActiveViewers");

		public static bool Exists => true
			&& OnMessageReceived != null
			&& GetViewer != null
			&& m_GetViewerCoins != null
			&& UnlimitedCoins != null;

		public static void RefreshViewers()
		{
			var usernames = ParseViewersFromJsonAndFindActiveViewers?.Invoke();
			if (usernames != null)
				foreach (string username in usernames)
					_ = GetViewer(username);
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

		public static string[] GetAllCommands()
		{
			var t_DefDatabase = typeof(DefDatabase<>).MakeGenericType(TypeByName("TwitchToolkit.Command"));
			return Traverse.Create(t_DefDatabase)
				.Field("defsList").GetValue<IEnumerable>().Cast<Def>()
				.Select(def => MakeDeepCopy<TTCommand>(def))
				.Where(cmd => !cmd.requiresAdmin && !cmd.requiresMod && cmd.enabled)
				.Select(cmd => cmd.command)
				.OrderBy(txt => txt)
				.ToArray();
		}

		static readonly FieldRef<bool> MinifiableBuildings = Tools.GetOptionalStaticFieldRef<bool>("TwitchToolkit.ToolkitSettings", "MinifiableBuildings");
		public static string[] GetFilteredItems(string searchTerm = null)
		{
			var minifiableBuildings = MinifiableBuildings();
			return DefDatabase<ThingDef>.AllDefs
				.Where(def => (def.tradeability.TraderCanSell() || ThingSetMakerUtility.CanGenerate(def)) && (def.building == null || def.Minifiable || minifiableBuildings) && (def.FirstThingCategory != null || def.race != null) && def.BaseMarketValue > 0f)
				.Select(def => def.label.Replace(" ", ""))
				.Where(name => searchTerm == null || searchTerm == "" || name.ToLower().Contains(searchTerm.ToLower()))
				.OrderBy(name => name)
				.ToArray();
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
		static class TwitchWrapper_SendChatMessage_Wrapper_Patches
		{
			static MethodInfo method;
			static Type iTwitchMessage;
			static MethodInfo iTwitchMessage_Username;
			static Type viewer;
			static FieldInfo viewer_username;

			static readonly Regex parser = new Regex("(.*)\\@([^ ]+)(.*)");
			static readonly Regex byRemover = new Regex(" by$");

			public static bool Prepare(MethodInfo original)
			{
				if (original == null)
				{
					method = Method("ToolkitCore.TwitchWrapper:SendChatMessage");
					iTwitchMessage = TypeByName("TwitchLib.Client.Models.Interfaces.ITwitchMessage");
					iTwitchMessage_Username = iTwitchMessage != null ? PropertyGetter(iTwitchMessage, "Username") : null;
					viewer = TypeByName("TwitchToolkit.Viewer");
					viewer_username = viewer != null ? Field(viewer, "username") : null;

					return method != null && iTwitchMessage_Username != null && viewer_username != null;
				}
				return true;
			}

			public static IEnumerable<MethodBase> TargetMethods()
			{
				var toolkitAssembly = TypeByName("TwitchToolkit.TwitchToolkit").Assembly;
				return toolkitAssembly.GetTypes().SelectMany(type => GetDeclaredMethods(type)).Where(caller =>
				{
					if (caller.IsAbstract) return false;
					if (caller.IsGenericMethod) return false;
					var parameterTypes = caller.GetParameters().Select(p => p.ParameterType);
					if (parameterTypes.FirstIndex(t => t == iTwitchMessage) >= 0) return true;
					if (parameterTypes.FirstIndex(t => t == viewer) >= 0) return true;
					if (PropertyGetter(caller.DeclaringType, "Viewer") != null) return true;
					if (caller.GetParameters().Any(p => p.ParameterType == typeof(string) && p.Name == "username")) return true;
					return false;
				}).Cast<MethodBase>();
			}

			public static void SendChatMessage(string message, string userName)
			{
				//Tools.LogWarning($"SendChatMessage [{userName}] [{message}]");

				var originalMessage = message;
				var match = parser.Match(message);
				if (match.Success)
				{
					userName = match.Groups[2].Value;

					var msg1 = match.Groups[1].Value.Trim();
					msg1 = byRemover.Replace(msg1, "");

					var mgs2 = match.Groups[3].Value.Trim();
					message = $"{msg1} {mgs2}".Trim();
				}

				var puppeteer = State.Instance.PuppeteerForViewerName(userName);
				if (puppeteer == null || puppeteer.IsConnected == false)
				{
					_ = method.Invoke(null, new object[] { originalMessage });
					return;
				}

				Controller.instance.SendChatMessage(puppeteer.vID, message);
				if (PuppeteerMod.Settings.sendChatResponsesToTwitch)
					_ = method.Invoke(null, new object[] { message });
			}

			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase caller)
			{
				var replacement = SymbolExtensions.GetMethodInfo(() => SendChatMessage("", ""));
				foreach (var instr in instructions)
				{
					if (instr.Calls(method))
					{
						int idx;
						var found = false;
						var parameterTypes = caller.GetParameters().Select(p => p.ParameterType);
						idx = parameterTypes.FirstIndex(t => t == iTwitchMessage);
						if (idx >= 0)
						{
							if (caller.IsStatic == false) idx++;
							yield return new CodeInstruction(OpCodes.Ldarg, idx);
							yield return new CodeInstruction(OpCodes.Call, iTwitchMessage_Username);
							found = true;
						}
						else
						{
							idx = parameterTypes.FirstIndex(t => t == viewer);
							if (idx >= 0)
							{
								if (caller.IsStatic == false) idx++;
								yield return new CodeInstruction(OpCodes.Ldarg, idx);
								yield return new CodeInstruction(OpCodes.Ldfld, viewer_username);
								found = true;
							}
							else if (PropertyGetter(caller.DeclaringType, "Viewer") != null && caller.IsStatic == false)
							{
								yield return new CodeInstruction(OpCodes.Ldarg_0);
								yield return new CodeInstruction(OpCodes.Ldfld, viewer_username);
								found = true;
							}
							else
							{
								idx = caller.GetParameters().FirstIndex(p => p.ParameterType == typeof(string) && p.Name == "username");
								if (idx >= 0)
								{
									if (caller.IsStatic == false) idx++;
									yield return new CodeInstruction(OpCodes.Ldarg, idx);
									found = true;
								}
							}
						}

						if (found)
							instr.operand = replacement;
					}
					yield return instr;
				}
			}
		}
	}

	class TTCommand
	{
#pragma warning disable 649
		public bool requiresAdmin;
		public bool requiresMod;
		public bool enabled;
		public string command;
#pragma warning restore 649
	}
}
