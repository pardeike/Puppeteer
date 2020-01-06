using System;
using System.IO;
using System.Text;
using Verse;

namespace Puppeteer
{
	public static class Tools
	{
		public static string Base64Decode(string value)
		{
			value = value.Replace('-', '+');
			value = value.Replace('_', '/');

			value = value.PadRight(value.Length + (4 - value.Length % 4) % 4, '=');

			var data = Convert.FromBase64String(value);
			return Encoding.UTF8.GetString(data);
		}

		public static string ReadConfig(string name)
		{
			var path = Path.Combine(GenFilePaths.ConfigFolderPath, name);
			if (File.Exists(path) == false) return null;
			return File.ReadAllText(path, Encoding.UTF8);
		}

		public static void WriteConfig(string name, string contents)
		{
			var path = Path.Combine(GenFilePaths.ConfigFolderPath, name);
			File.WriteAllText(path, contents);
		}
	}
}