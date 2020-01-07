using JsonFx.Json;
using System;

namespace Puppeteer
{
	public class Colonist
	{
		public ViewerID controller;
		[JsonIgnore] public DateTime lastSeen = DateTime.MinValue;
	}
}