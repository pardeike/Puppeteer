using JsonFx.Json;
using System.Text;

namespace Puppeteer
{
	public abstract class JSONConvertable<T>
	{
		public string GetJSON()
		{
			var sb = new StringBuilder();
			using (var writer = new JsonWriter(sb)) { writer.Write(this); }
			return sb.ToString();
		}

		public static T Create(string str)
		{
			var reader = new JsonReader(str);
			return reader.Deserialize<T>();
		}
	}
}