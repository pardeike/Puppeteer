using Newtonsoft.Json;

namespace Puppeteer
{
	public class ViewerID
	{
		public string id;
		public string service;
		public string name;
		public string picture;

		[JsonIgnore] public string Identifier => $"{service}:{id}";
		[JsonIgnore] public bool IsValid => (id ?? "").Length > 0 && (service ?? "").Length > 0;
		[JsonIgnore] public ViewerID Simple => new ViewerID() { id = id, service = service };

		public ViewerID() { }

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
			return $"{name}:{service}:{id}";
		}
	}
}