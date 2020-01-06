using System.Diagnostics;
using System.Timers;
using Verse;

namespace Puppeteer
{
	public enum Event
	{
		GameEntered,
		GameExited,
		Save
	}

	public interface ICommandProcessor
	{
		void Message(string msg);
	}

	public class Puppeteer : ICommandProcessor
	{
		readonly Timer earnTimer = new Timer(earnIntervalInSeconds * 1000) { AutoReset = true };

		const bool developmentMode = true;
		const int earnIntervalInSeconds = 2;
		const int earnAmount = 10;

		Connection connection;
		readonly Viewers viewers;

		public Puppeteer()
		{
			earnTimer.Elapsed += new ElapsedEventHandler((sender, e) =>
			{
				if (Find.CurrentMap != null)
					viewers.Earn(connection, earnAmount);
			});
			earnTimer.Start();
			viewers = new Viewers();
		}

		~Puppeteer()
		{
			earnTimer?.Stop();
		}

		public void SetEvent(Event evt)
		{
			switch(evt)
			{
				case Event.GameEntered:
					connection = new Connection(developmentMode, this);
					break;
				case Event.GameExited:
					if (connection != null)
					{
						connection.Disconnect();
						connection = null;
					}
					break;
				case Event.Save:
					viewers.Save();
					break;
			}
		}

		public void Message(string msg)
		{
			try
			{
				// Log.Warning($"MSG {msg}");
				var cmd = SimpleCmd.Create(msg);
				switch (cmd.type)
				{
					case "welcome":
						break;
					case "join":
						var join = Join.Create(msg);
						viewers.Join(connection, join.viewer);
						break;
					case "leave":
						var leave = Leave.Create(msg);
						viewers.Leave(leave.viewer);
						break;
					default:
						Log.Warning($"unknown command '{cmd.type}'");
						break;
				}
			}
			catch (System.Exception e)
			{
				Log.Warning($"While handling {msg}: {e}");
			}
		}

		static long secs = 0;
		static long min = 1000000;
		static long max = -100000;
		static long n = 0;
		static int fail = 0;
		static int counter = 0;
		public void PawnUpdate(Pawn pawn)
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			var data = new Update() { data = new DataJSON(pawn) }.GetJSON();
			connection.Send(data, (success) =>
			{
				var d = stopWatch.ElapsedMilliseconds;
				stopWatch.Stop();
				if (d < min) min = d;
				if (d > max) max = d;
				if (success == false) fail++;
				secs += d;
				n++;
			});

			if (++counter >= 60)
			{
				var avg = (float)secs / n;
				Log.Warning("-> avg:" + avg + " min:" + min + " max:" + max + " fail:" + fail);
				secs = 0;
				n = 0;
				min = 1000000;
				max = -100000;
				counter = 0;
				fail = 0;
			}
		}
	}
}