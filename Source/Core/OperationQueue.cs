using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse.Noise;

namespace Puppeteer.Core
{
	public enum OperationType
	{
		Portrait
	}

	public static class OperationQueue
	{
		static readonly Dictionary<OperationType, ConcurrentQueue<Action>> state = new Dictionary<OperationType, ConcurrentQueue<Action>>();

		public static void Add(OperationType type, Action action)
		{
			if (state.TryGetValue(type, out var queue) == false)
			{
				queue = new ConcurrentQueue<Action>(true);
				state[type] = queue;
			}
			queue.Enqueue(action);
		}

		public static void Process(OperationType type)
		{
			if (state.TryGetValue(type, out var queue))
				queue.Dequeue()?.Invoke();
		}
	}
}