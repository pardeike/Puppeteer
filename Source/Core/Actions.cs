using System;
using System.Collections.Generic;
using Verse;

namespace Puppeteer
{
	static class Actions
	{
		static readonly Dictionary<Pawn, Dictionary<string, KeyValuePair<string, Action>>> allActions = new Dictionary<Pawn, Dictionary<string, KeyValuePair<string, Action>>>();

		public static void AddAction(Pawn pawn, string id, FloatMenuOption choice)
		{
			if (allActions.TryGetValue(pawn, out var actions) == false)
			{
				actions = new Dictionary<string, KeyValuePair<string, Action>>();
				allActions[pawn] = actions;
			}
			actions[id] = new KeyValuePair<string, Action>(choice.Label, choice.action);
		}

		public static bool RunAction(Pawn pawn, string id)
		{
			if (allActions.TryGetValue(pawn, out var actions) == false)
				return false;
			if (actions.TryGetValue(id, out var pair) == false)
				return false;
			pawn.RemoteLog(pair.Key);
			pair.Value();
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
