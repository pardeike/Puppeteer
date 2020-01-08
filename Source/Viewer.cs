using JsonFx.Json;
using Verse;

namespace Puppeteer
{
	public class Viewer
	{
		public ViewerID vID;
		[JsonIgnore] public bool connected = false;
		[JsonIgnore] public Pawn controlling = null;
		public int coins = 0;

		public override string ToString()
		{
			return $"Viewer {vID} controlling {controlling?.Name.ToStringShort ?? "---"}{(connected ? " connected" : "")}";
		}
	}
}