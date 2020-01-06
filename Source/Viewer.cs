using JsonFx.Json;

namespace Puppeteer
{
	public class Viewer
	{
		public ViewerID vID;
		public string name = null;
		[JsonIgnore] public bool connected = true;
		public int coins = 0;
	}
}