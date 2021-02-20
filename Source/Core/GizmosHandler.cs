using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class GizmosHandler
	{
		public class GizmoAction
		{
			public string label;
			public Thing target;
			public Action action;
		}

		static readonly Event mouseClick = new Event(0) { type = EventType.MouseDown, button = 0, clickCount = 1 };
		static readonly Dictionary<Pawn, Dictionary<string, GizmoAction>> allActions = new Dictionary<Pawn, Dictionary<string, GizmoAction>>();
		static readonly Dictionary<Command, Command> actionCommands = new Dictionary<Command, Command>();

		[HarmonyPatch(typeof(BuildCopyCommandUtility))]
		[HarmonyPatch(nameof(BuildCopyCommandUtility.BuildCommand))]
		static class BuildCopyCommandUtility_BuildCommand_Patch
		{
			static Command_Action Register(Command_Action action, Designator_Build buildCmd)
			{
				actionCommands[action] = buildCmd;
				return action;
			}

			[HarmonyPriority(Priority.First)]
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var m_FindAllowedDesignator = SymbolExtensions.GetMethodInfo(() => BuildCopyCommandUtility.FindAllowedDesignator(default, false));
				var m_Register = SymbolExtensions.GetMethodInfo(() => Register(default, default));

				var list = instructions.ToList();
				var idx = instructions.FirstIndex(inst => inst.Calls(m_FindAllowedDesignator));
				if (idx < 0)
				{
					Log.Error("Cannot find BuildCopyCommandUtility.FindAllowedDesignator in BuildCopyCommandUtility.BuildCommand");
					return instructions;
				}
				var fld = list[idx + 1].operand;
				if (list.Last().opcode != OpCodes.Ret)
				{
					Log.Error("BuildCopyCommandUtility.BuildCommand does not end with RET");
					return instructions;
				}
				var labels = list.Last().labels;
				list.Last().labels = new List<Label>();
				list.InsertRange(list.Count - 1, new[]
				{
					new CodeInstruction(OpCodes.Ldloc_0) { labels = labels },
					new CodeInstruction(OpCodes.Ldfld, fld),
					new CodeInstruction(OpCodes.Call, m_Register)
				});
				return list.AsEnumerable();
			}
		}

		public static void AddAction(Pawn pawn, string id, GizmosHandler.Item gizmo, Thing target)
		{
			if (allActions.TryGetValue(pawn, out var actions) == false)
			{
				actions = new Dictionary<string, GizmoAction>();
				allActions[pawn] = actions;
			}
			actions[id] = new GizmoAction() { label = gizmo.label, target = target, action = gizmo.action };
		}

		public static bool RunAction(Pawn pawn, string id)
		{
			if (allActions.TryGetValue(pawn, out var actions) == false)
				return false;
			if (actions.TryGetValue(id, out var tuple) == false)
				return false;
			pawn.RemoteLog(tuple.label, tuple.target);
			tuple.action();
			_ = actions.Remove(id);
			return true;
		}

		public static void RemoveActions(Pawn pawn)
		{
			if (allActions.TryGetValue(pawn, out var actions) == false)
				return;
			actions.Clear();
		}

		public class Item
		{
			public string label;
			public Action action;
			public Texture2D icon;
			public string disabled;
			public bool allowed;
			public float order;
		}

		/*
			if (Find.Selector.NumSelected == 1)
			{
				var thing = Find.Selector.SingleSelectedThing;
				var actions = GizmoCapture.GetActions(thing);
				Tools.LogWarning($"Gizmos for {thing}");
				foreach (var action in actions)
					Tools.LogWarning($"=> {action.label} {action.icon.width} x {action.icon.height}");
			}
		*/

		public static List<Command> GetCommands(Pawn pawn, IntVec3 cell, object obj)
		{
			if (obj == null) return null;
			var result = new List<Command>();
			if (obj is Thing thing)
				result.AddRange(Find.ReverseDesignatorDatabase.AllDesignators
					.Where(des => des.CanDesignateThing(thing).Accepted));
			if (obj is ISelectable selectable)
			{
				actionCommands.Clear();

				var cmds = Tools.GetCommands(selectable);
				cmds.Do(cmd =>
				{
					var reason = Tools.Restricted(pawn, cell, cmd.LabelCap);
					if (reason != null)
						cmd.Disable(reason);
				});
				result.AddRange(cmds);
			}
			return result
				.OrderBy(gizmo => gizmo.order).ToList();
		}

		public static List<Item> GetActions(object obj, List<Command> commands)
		{
			var count = Widgets.mouseOverScrollViewStack;

			bool Allowed(Command cmd)
			{
				if (actionCommands.TryGetValue(cmd, out var realCmd))
					cmd = realCmd;

				if ((cmd as Designator_Place) != null) return false;
				if ((cmd as Designator_Build) != null) return false;
				return true;
			}

			var result = commands
				.Select(cmd =>
				{
					_ = actionCommands.TryGetValue(cmd, out var realCmd);
					if (cmd is Designator des)
					{
						var thing = obj as Thing;
						return new Item
						{
							label = des.LabelCapReverseDesignating(thing),
							icon = des.IconReverseDesignating(thing, out var angle, out var offset),
							order = ((des is Designator_Uninstall) ? (-11f) : (-20f)),
							disabled = des.disabled ? des.disabledReason : null,
							allowed = Allowed(cmd),
							action = delegate
							{
								des.DesignateThing(thing);
								des.Finalize(true);
							}
						};
					}
					return new Item
					{
						label = cmd.LabelCap,
						icon = cmd.icon,
						order = ((cmd is Designator_Uninstall) ? (-11f) : (-20f)),
						disabled = cmd.disabled ? cmd.disabledReason : null,
						allowed = Allowed(cmd),
						action = () => cmd.ProcessInput(mouseClick)
					};
				})
				.ToList();

			Widgets.mouseOverScrollViewStack = count;

			return result;
		}
	}
}
