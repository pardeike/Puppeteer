using System;
using System.Collections.Generic;
using Verse;

namespace Puppeteer
{
	static class Actions
	{
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
	}
}
