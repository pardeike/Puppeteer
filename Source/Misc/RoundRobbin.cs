﻿using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Puppeteer
{
	public class RoundRobbin
	{
		static readonly Dictionary<string, RoundRobbin> state = new Dictionary<string, RoundRobbin>();

		int ticks = 0;
		float interval;
		float delay = 10;
		int counter = -1;

		public static void Create(string name, float interval = 60f)
		{
			state[name] = new RoundRobbin() { interval = interval, delay = interval };
		}

		public static Pawn NextColonist(string name)
		{
			if (state.TryGetValue(name, out var robbin) == false) return null;

			robbin.ticks++;
			if (robbin.ticks < robbin.delay) return null;
			robbin.ticks = 0;

			var colonists = Current.Game.Maps.SelectMany(map => PlayerPawns.FreeColonists(map)).ToList();
			if (colonists.Count == 0) return null;
			robbin.delay = robbin.interval / colonists.Count + 1;

			var idx = (robbin.counter + 1) % colonists.Count;
			robbin.counter = idx;
			return colonists[idx];
		}
	}
}