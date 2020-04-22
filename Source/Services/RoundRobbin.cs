using System.Collections.Generic;
using System.Linq;

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

		public static State.Puppeteer NextColonist(string name)
		{
			if (state.TryGetValue(name, out var robbin) == false) return null;

			robbin.ticks++;
			if (robbin.ticks < robbin.delay) return null;
			robbin.ticks = 0;

			var puppeteers = State.Instance.ConnectedPuppeteers().ToList();
			if (puppeteers.Count == 0) return null;
			robbin.delay = robbin.interval / puppeteers.Count + 1;

			var idx = (robbin.counter + 1) % puppeteers.Count;
			robbin.counter = idx;
			return puppeteers[idx];
		}
	}
}