using Newtonsoft.Json;

namespace Puppeteer
{
	public class Colonist
	{
		public ViewerID controller;
		[JsonIgnore] public string lastSeen = "";
		[JsonIgnore] public byte[] portrait = null;

		public override string ToString()
		{
			return $"Colonist {controller}{((lastSeen?.Length ?? 0) > 0 ? "" : $" last seen {lastSeen}")}]";
		}
	}
}