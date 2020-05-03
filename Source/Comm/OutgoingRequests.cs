using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class OutgoingRequests
	{
		class SendTask
		{
			public string type;
			public byte[] data;
			public Action<bool> callback;
		}

		public static int MaxQueued = 200;
		static readonly RunningAverage runningAverage = new RunningAverage(MaxQueued / 10);
		static readonly System.Timers.Timer periodical = new System.Timers.Timer(500) { AutoReset = true };
		static readonly Stopwatch stopwatch = new Stopwatch();
		public static long AverageSendTime { get; private set; } = 0;
		public static long ErrorCount = 0;

		static readonly ConcurrentQueue<SendTask> tasks = new ConcurrentQueue<SendTask>();
		public static int Count => tasks.Count;

		static OutgoingRequests()
		{
			periodical.Elapsed += new System.Timers.ElapsedEventHandler((sender, e) => { if (ErrorCount > 0) ErrorCount--; });
			periodical.Start();

			var thread = new Thread(Process);
			thread.Start();
		}

		public static void Add(string type, byte[] data, Action<bool> callback)
		{
			if (tasks.Count >= MaxQueued) return;
			var task = new SendTask() { type = type, data = data, callback = callback };
			tasks.Enqueue(task);
		}

		public static void Clear()
		{
			while (tasks.TryDequeue(out var _)) ;
		}

		static Task Send(SendTask task)
		{
			if (task == null) return Task.Delay(5);
			var ws = Controller.instance.connection?.ws;
			if (ws == null) return Task.Delay(100);
			return Task.Run(() =>
			{
				stopwatch.Start();
				ws.SendAsync(task.data, success =>
				{
					AverageSendTime = runningAverage.Add(stopwatch.ElapsedMilliseconds);
					stopwatch.Reset();
					if (success == false)
					{
						ErrorCount++;
						Tools.LogWarning($"Error sending {task.type}");
					}
					task.callback(success);
				});
			});
		}

		static async void Process()
		{
			while (true)
			{
				_ = tasks.TryDequeue(out var task);
				await Send(task);
				if (ErrorCount > 0) ErrorCount--;
			}
		}
	}
}