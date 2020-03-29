using System;
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
		void Message(byte[] msg);
	}

	[StaticConstructorOnStartup]
	public class Puppeteer : ICommandProcessor
	{
		public static Puppeteer instance = new Puppeteer();

		readonly Timer earnTimer = new Timer(earnIntervalInSeconds * 1000) { AutoReset = true };
		public Timer connectionRetryTimer = new Timer(10000) { AutoReset = true };

		const bool developmentMode = true;
		const int earnIntervalInSeconds = 2;
		const int earnAmount = 10;

		public Connection connection;
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

			connectionRetryTimer.Elapsed += new ElapsedEventHandler((sender, e) =>
			{
				connection?.Send(new Ping());
			});
			connectionRetryTimer.Start();
		}

		~Puppeteer()
		{
			connectionRetryTimer?.Stop();
			earnTimer?.Stop();
		}

		public void SetEvent(Event evt)
		{
			switch (evt)
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
			return new ViewerInfo()
			{
				controller = colonist.controller,
				pawn = viewer.controlling,
				connected = viewer.connected
			};
		}

		public void Message(byte[] msg)
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
						colonists.Assign(assign.colonistID, assign.viewer, connection);
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

		public void PawnOnMap(Pawn pawn, byte[] image)
		{
			if (connection == null) return;
			var viewerInfo = GetViewerInfo(pawn);
			if (viewerInfo == null || viewerInfo.controller == null) return;
			connection.Send(new OnMap() { viewer = viewerInfo.controller, info = new OnMap.Info() { image = image } });
		}

		public void UpdatePortrait(Pawn pawn)
		{
			var colonist = colonists.FindColonist(pawn);
			if (colonist == null || colonist.controller == null) return;
			var viewer = viewers.FindViewer(colonist.controller);
			if (viewer == null || viewer.controlling == null) return;
			Viewers.SendPortrait(connection, viewer);
		}

		public void UpdateColonist(Pawn pawn)
		{
			var colonist = colonists.FindColonist(pawn);
			if (colonist == null || colonist.controller == null) return;
			var viewer = viewers.FindViewer(colonist.controller);
			if (viewer == null || viewer.controlling == null) return;

			var inspect = Array.Empty<string>();
			var info = new ColonisBaseInfo.Info();
			if (pawn.Spawned && pawn.Map != null)
			{
				inspect = pawn.GetInspectString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
				info.name = pawn.Name.ToStringFull;
				info.x = pawn.Position.x;
				info.y = pawn.Position.z;
				info.mx = pawn.Map.Size.x;
				info.my = pawn.Map.Size.z;
				info.inspect = inspect;
			}
			connection.Send(new ColonisBaseInfo() { viewer = viewer.vID, info = info });
		}
	}
}