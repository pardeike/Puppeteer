using System;
using System.Collections.Concurrent;

namespace Puppeteer
{
	public enum OperationType
	{
		Portrait,
		SetState,
		Job,
		Log
	}

	public static class OperationQueue
	{
		static readonly ConcurrentDictionary<OperationType, ConcurrentQueue<Action>> state = new ConcurrentDictionary<OperationType, ConcurrentQueue<Action>>();

		public static void Add(OperationType type, Action action)
		{
			if (state.TryGetValue(type, out var queue) == false)
			{
				queue = new ConcurrentQueue<Action>();
				_ = state.TryAdd(type, queue);
			}
			queue.Enqueue(action);
		}

		public static void Process(OperationType type)
		{
			if (state.TryGetValue(type, out var queue))
			{
				try
				{
					if (queue.TryDequeue(out var item))
						item.Invoke();
				}
				catch (Exception e)
				{
					Tools.LogWarning($"While dequeuing {type}: {e}");
				}
			}
		}
	}
}