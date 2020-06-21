using System;
using Verse;

namespace Puppeteer
{
	public class MapFaker : IDisposable
	{
		bool disposed = false;
		readonly sbyte savedMapIndex;

		public MapFaker(Pawn pawn)
		{
			var game = Current.Game;
			savedMapIndex = game.currentMapIndex;
			if (pawn != null) game.currentMapIndex = (sbyte)pawn.Map.Index;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing)
				Current.Game.currentMapIndex = savedMapIndex;

			disposed = true;
		}
	}
}