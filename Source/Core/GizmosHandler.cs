using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class GizmosHandler
	{
		static readonly Event mouseClick = new Event(0) { type = EventType.MouseDown, button = 0, clickCount = 1 };
		static readonly Dictionary<Pawn, Dictionary<string, Action>> allActions = new Dictionary<Pawn, Dictionary<string, Action>>();

		public static void AddAction(Pawn pawn, string id, Action action)
		{
			if (allActions.TryGetValue(pawn, out var actions) == false)
			{
				actions = new Dictionary<string, Action>();
				allActions[pawn] = actions;
			}
			actions[id] = action;
		}

		public static bool RunAction(Pawn pawn, string id)
		{
			if (allActions.TryGetValue(pawn, out var actions) == false)
				return false;
			if (actions.TryGetValue(id, out var action) == false)
				return false;
			action();
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

		public static List<Command> GetCommands(object obj)
		{
			if (obj == null) return null;
			var result = new List<Command>();
			if (obj is Thing thing)
				result.AddRange(Find.ReverseDesignatorDatabase.AllDesignators
					.Where(des => des.CanDesignateThing(thing).Accepted));
			if (obj is ISelectable selectable)
				result.AddRange(selectable.GetGizmos().Cast<Command>());
			return result.OrderBy(gizmo => gizmo.order).ToList();
		}

		public static List<Item> GetActions(object obj, List<Command> commands)
		{
			var count = Widgets.mouseOverScrollViewStack;

			var result = commands.Select(cmd =>
			{
				if (cmd is Designator des)
				{
					var thing = obj as Thing;
					return new Item
					{
						label = des.LabelCapReverseDesignating(thing),
						icon = des.IconReverseDesignating(thing, out var angle, out var offset),
						order = ((des is Designator_Uninstall) ? (-11f) : (-20f)),
						disabled = des.disabled ? des.disabledReason : null,
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
					action = () => cmd.ProcessInput(mouseClick)
				};
			}).ToList();

			Widgets.mouseOverScrollViewStack = count;

			return result;
		}
	}
}
