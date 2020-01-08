using JsonFx.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;

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

	public class ViewerInfo
	{
		public ViewerID controller;
		public Pawn pawn;
		public bool connected;
	}

	public class ViewerID
	{
		public string id;
		public string service;
		public string name;
		public string picture;

		public string Identifier => $"{service}:{id}";
		public bool IsValid => (id ?? "").Length > 0 && (service ?? "").Length > 0;
		public ViewerID Simple => new ViewerID() { id = id, service = service };

		public ViewerID() {}

		public ViewerID(string str)
		{
			var parts = str.Split(':');
			service = parts[0];
			id = parts[1];
			name = parts.Length > 2 ? parts[2] : null;
			picture = null;
		}


		public override int GetHashCode()
		{
			return (id.GetHashCode() * 397) ^ service.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as ViewerID);
		}

		public bool Equals(ViewerID obj)
		{
			return obj == this;
		}

		public static bool operator ==(ViewerID v1, ViewerID v2)
		{
			if (((object)v1) == null)
				return ((object)v2) == null;
			if (((object)v2) == null)
				return ((object)v1) == null;
			return v1.id == v2.id && v1.service == v2.service;
		}

		public static bool operator !=(ViewerID v1, ViewerID v2)
		{
			return !(v1 == v2);
		}

		public override string ToString()
		{
			return $"{service}:{id}:{name}{(picture != null ? ":P" : "")}";
		}
	}
}