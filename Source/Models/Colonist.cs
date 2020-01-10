using JsonFx.Json;

namespace Puppeteer
{
	public class Colonist
	{
		public ViewerID controller;
		[JsonIgnore] public string lastSeen = "";

		public override string ToString()
		{
			return $"Colonist {controller}{((lastSeen?.Length ?? 0) > 0 ? "" : $" last seen {lastSeen}")}]";
		}
	}
}