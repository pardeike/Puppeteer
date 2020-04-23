using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.IO;

namespace Puppeteer
{
	public abstract class JSONConvertable<T>
	{
		public string type;

		public byte[] GetData()
		{
			var ms = new MemoryStream();
			using (var writer = new BsonWriter(ms))
			{
				var serializer = new JsonSerializer();
				serializer.Serialize(writer, this);
			}
			return ms.ToArray();
		}

		public static T Create(byte[] data)
		{
			var ms = new MemoryStream(data);
			using (var reader = new BsonReader(ms))
			{
				var serializer = new JsonSerializer();
				return serializer.Deserialize<T>(reader);
			}
		}
	}
}