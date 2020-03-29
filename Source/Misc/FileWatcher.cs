using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using Verse;

namespace Puppeteer
{
	[StaticConstructorOnStartup]
	public static class FileWatcher
	{
		static readonly FileSystemWatcher fsw;
		static readonly List<Action<string, string>> actions = new List<Action<string, string>>();

		static FileWatcher()
		{
			fsw = new FileSystemWatcher
			{
				Path = GenFilePaths.ConfigFolderPath
			};
			fsw.Created += Change("created");
			fsw.Changed += Change("changed");
			fsw.Deleted += Change("deleted");
			fsw.EnableRaisingEvents = true;
		}

		static FileSystemEventHandler Change(string type)
		{
			return (object sender, FileSystemEventArgs e) =>
			{
				var filename = Path.GetFileName(e.FullPath);
				actions.Do(action => action(type, filename));
			};
		}

		public static Action<string, string> AddListener(Action<string, string> action)
		{
			actions.Add(action);
			return action;
		}

		public static void RemoveListener(Action<string, string> action)
		{
			_ = actions.Remove(action);
		}
	}
}