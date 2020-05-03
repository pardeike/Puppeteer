using System;
using System.Collections.Concurrent;
using System.Threading;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class BackgroundOperations
	{
		static readonly ConcurrentQueue<Action<Connection>> operations = new ConcurrentQueue<Action<Connection>>();
		public static int Count => operations.Count;

		static BackgroundOperations()
		{
			var thread = new Thread(Process)
			{
				Priority = ThreadPriority.Lowest,
				IsBackground = true,
				Name = "BackgroundOperations"

			};
			thread.Start();
		}

		public static void Add(Action<Connection> operation)
		{
			operations.Enqueue(operation);
		}

		public static void Clear()
		{
			while (operations.TryDequeue(out var _)) ;
		}

		static void Process()
		{
			while (true)
			{
				_ = operations.TryDequeue(out var operation);
				if (operation == null)
				{
					Thread.Sleep(10);
					continue;
				}
				var connection = Controller.instance.connection;
				if (connection == null || connection.isConnected == false)
				{
					Thread.Sleep(100);
					continue;
				}
				try
				{
					operation(connection);
				}
				catch (Exception e)
				{
					Log.Error($"Background operation error: {e}");
				}
			}
		}
	}
}