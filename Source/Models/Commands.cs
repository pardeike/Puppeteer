using System.Collections.Generic;

namespace Puppeteer
{
	public class SimpleCmd : JSONConvertable<SimpleCmd>
	{
		public string type;
	}

	public class Ping : JSONConvertable<Earned>
	{
		public string type = "ping";
		public bool game = true;
	}

	public class Earned : JSONConvertable<Earned>
	{
		public class Info
		{
			public int amount;
		}

		public string type = "earn";
		public ViewerID viewer;
		public Info info;
	}

	public class Portrait : JSONConvertable<Portrait>
	{
		public class Info
		{
			public byte[] image;
		}

		public string type = "portrait";
		public ViewerID viewer;
		public Info info;
	}

	public class OnMap : JSONConvertable<OnMap>
	{
		public class Info
		{
			public byte[] image;
		}

		public string type = "on-map";
		public ViewerID viewer;
		public Info info;
	}

	public class Assignment : JSONConvertable<Assignment>
	{
		public string type = "assignment";
		public ViewerID viewer;
		public bool state;
	}

	public class Join : JSONConvertable<Join>
	{
		public string type;
		public ViewerID viewer;
	}

	public class Leave : JSONConvertable<Leave>
	{
		public string type;
		public ViewerID viewer;
	}

	public class Assign : JSONConvertable<Assign>
	{
		public string type;
		public int colonistID;
		public ViewerID viewer;
	}

	public class Update : JSONConvertable<Update>
	{
		public string type = "update";
		public ViewerID viewer;
		public DataJSON data;
	}

	public class ColonistInfo : JSONConvertable<ColonistInfo>
	{
		public int id;
		public string name;
		public ViewerID controller;
		public string lastSeen;
	}

	public class AllColonists : JSONConvertable<AllColonists>
	{
		public string type = "colonists";
		public List<ColonistInfo> colonists;
	}
}