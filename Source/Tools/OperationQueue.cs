using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Puppeteer
{
	public enum OperationType
	{
		Portrait,
		SetState,
		Job,
		Log,
		Select,
		RenderMap,
		SocialRelations,
		Gear,
		Inventory
	}

	public struct Operation
	{
		public string name;
		public Action action;

		public Operation(Action action)
		{
			name = "";
			this.action = action;
		}
	}

	public static class OperationQueue
	{
		static readonly ConcurrentDictionary<OperationType, ConcurrentQueue<Operation>> state = new ConcurrentDictionary<OperationType, ConcurrentQueue<Operation>>();

		public static void Add(OperationType type, Action action)
		{
			if (state.TryGetValue(type, out var queue) == false)
			{
				queue = new ConcurrentQueue<Operation>();
				_ = state.TryAdd(type, queue);
			}
			queue.Enqueue(new Operation(action));
		}

		public static void Add(OperationType type, Operation operation)
		{
			if (state.TryGetValue(type, out var queue) == false)
			{
				queue = new ConcurrentQueue<Operation>();
				_ = state.TryAdd(type, queue);
			}
			lock (queue)
			{
				if (operation.name == "" || queue.Where(item => item.name == operation.name).Any() == false)
					queue.Enqueue(operation);
			}
		}

		public static void Process(OperationType type)
		{
			if (state.TryGetValue(type, out var queue))
			{
				try
				{
					if (queue.TryDequeue(out var item))
						item.action.Invoke();
				}
				catch (Exception e)
				{
					Tools.LogWarning($"While dequeuing {type}: {e}");
				}
			}
		}
	}
}
