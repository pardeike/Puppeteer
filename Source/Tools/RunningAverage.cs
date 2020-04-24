using System;

namespace Puppeteer
{
	public class RunningAverage
	{
		readonly int size;
		readonly long[] values;
		long sum = 0;

		int valuesIndex = 0;
		int valueCount = 0;

		public RunningAverage(int size)
		{
			this.size = Math.Max(size, 1);
			values = new long[size];
		}

		public long Add(long newValue)
		{
			var temp = newValue - values[valuesIndex];
			values[valuesIndex] = newValue;
			sum += temp;

			valuesIndex++;
			valuesIndex %= size;
			if (valueCount < size)
				valueCount++;

			return sum / valueCount;
		}
	}
}