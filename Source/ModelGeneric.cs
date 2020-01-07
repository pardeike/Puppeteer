using JsonFx.Json;
using System;
using System.Collections.Generic;
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
		public string colonistID;
		public ViewerID viewer;
	}

	public class Connected: JSONConvertable<Connected>
	{
		public string type = "connected";
		public ViewerID viewer;
		public string colonistID;
		public bool state;
	}

	public class KeepAlive: JSONConvertable<KeepAlive>
	{
		public string type;
		public ViewerID viewer;
		public string colonistID;
	}

	public class Update : JSONConvertable<Update>
	{
		public string type = "update";
		public DataJSON data;
	}

	public class ColonistInfo : JSONConvertable<ColonistInfo>
	{
		public string id;
		public string name;
		public ViewerID controller;
		public double lastSeen;
	}

	public class AllColonists : JSONConvertable<AllColonists>
	{
		public string type = "colonists";
		public List<ColonistInfo> colonists;
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
			return obj != null && obj.id == id && obj.service == service;
		}

		public override string ToString()
		{
			return $"{service}:{id}:{name}{(picture != null ? ":P" : "")}";
		}
	}
}