using System.Collections.Generic;

namespace Puppeteer
{
	public class SimpleCmd : JSONConvertable<SimpleCmd>
	{
		public string type;
	}

	public class Earned : JSONConvertable<Earned>
	{
		public string type = "earn";
		public ViewerID viewer;
		public int amount;
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