using System.Collections.Generic;

namespace Puppeteer
{
	public class AttackResult
	{
		public class Result
		{
			public string name;
			public int id;
		}

		public List<Result> results;
	}

	public class ItemResult
	{
		public class Result
		{
			public string name;
			public int id;
			public bool selected;
		}

		public List<Result> results;
	}
}