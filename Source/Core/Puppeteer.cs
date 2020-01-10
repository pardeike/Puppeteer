using System.Diagnostics;
using System.Timers;
using Verse;

namespace Puppeteer
{
	public enum Event
	{
		GameEntered,
		GameExited,
		Save,
		ColonistsChanged
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
		readonly Colonists colonists;
		bool firstTime = true;

		public Puppeteer()
		{
			earnTimer.Elapsed += new ElapsedEventHandler((sender, e) =>
			{
				if (Find.CurrentMap != null)
					viewers.Earn(connection, earnAmount);
			});
			earnTimer.Start();
			viewers = new Viewers();
			colonists = new Colonists();
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
					connection?.Disconnect();
					connection = null;
					break;
				case Event.Save:
					viewers.Save();
					colonists.Save();
					break;
				case Event.ColonistsChanged:
					if (firstTime == false)
						colonists.SendAllColonists(connection);
					firstTime = false;
					break;
			}
		}

		public ViewerInfo GetViewerInfo(Pawn pawn)
		{
			var colonist = colonists.FindColonist(pawn);
			if (colonist == null || colonist.controller == null) return null;
			var viewer = viewers.FindViewer(colonist.controller);
			if (viewer == null || viewer.controlling == null) return null;
			return new ViewerInfo() { 
				controller = colonist.controller, 
				pawn = viewer.controlling,
				connected = viewer.connected
			};
		}

		public void Message(string msg)
		{
			if (connection == null) return;
			try
			{
				// Log.Warning($"MSG {msg}");
				var cmd = SimpleCmd.Create(msg);
				switch (cmd.type)
				{
					case "welcome":
						colonists.SendAllColonists(connection);
						break;
					case "join":
						var join = Join.Create(msg);
						viewers.Join(connection, colonists, join.viewer);
						break;
					case "leave":
						var leave = Leave.Create(msg);
						viewers.Leave(leave.viewer);
						break;
					case "assign":
						var assign = Assign.Create(msg);
						colonists.Assign(assign.colonistID, assign.viewer);
						colonists.SendAllColonists(connection);
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
			if (connection == null) return;

			var viewerInfo = GetViewerInfo(pawn);
			if (viewerInfo == null || viewerInfo.controller == null) return;

			var stopWatch = new Stopwatch();
			stopWatch.Start();

			var data = new Update() { viewer = viewerInfo.controller, data = new DataJSON(pawn) }.GetJSON();
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