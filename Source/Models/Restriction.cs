using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Verse;

namespace Puppeteer
{
	public enum Anchor
	{
		Contains,
		Starts,
		Ends,
		Is,
		RegExp
	}

	public class Matcher : IExposable
	{
		private Regex regex;
		public string text;
		public Anchor anchor;
		public bool caseSensitive;

		public Matcher()
		{
		}

		public Matcher(string text, Anchor anchor, bool caseSensitive)
		{
			this.text = text;
			this.anchor = anchor;
			this.caseSensitive = caseSensitive;
			regex = new Regex(text, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
		}

		public void UpdateRegex()
		{
			var regText = text;
			switch (anchor)
			{
				case Anchor.Contains:
					regText = Regex.Escape(text);
					break;
				case Anchor.Starts:
					regText = $"^{Regex.Escape(text)}";
					break;
				case Anchor.Ends:
					regText = $"{Regex.Escape(text)}$";
					break;
				case Anchor.Is:
					regText = $"^{Regex.Escape(text)}$";
					break;
			}
			regex = new Regex(regText, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref text, "text");
			Scribe_Values.Look(ref anchor, "anchor");
			Scribe_Values.Look(ref caseSensitive, "caseSensitive");

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
				UpdateRegex();
		}

		public bool IsMatch(string text)
		{
			return regex.IsMatch(text);
		}
	}

	public class Restriction : IExposable, ILoadReferenceable
	{
		string id = Guid.NewGuid().ToString();
		public string label = "Untitled";
		public List<Matcher> matchers = new List<Matcher>();

		public void AddRestriction(string text, Anchor anchor = Anchor.Contains, bool caseSensitive = false)
		{
			matchers.Add(new Matcher(text, anchor, caseSensitive));
		}

		public bool IsRestricted(string text)
		{
			return matchers.Any(r => r.IsMatch(text));
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref id, "id");
			Scribe_Values.Look(ref label, "label");
			Scribe_Collections.Look(ref matchers, "matchers", LookMode.Deep, Array.Empty<Matcher>());
		}

		public string GetUniqueLoadID()
		{
			return id;
		}
	}
}