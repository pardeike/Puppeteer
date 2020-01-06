using JsonFx.Json;

namespace Puppeteer
{
	public class Colonist
	{
		public ViewerID controller;
		[JsonIgnore] public bool connected = false;
	}
}