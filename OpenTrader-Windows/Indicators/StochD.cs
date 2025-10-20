// The stochastic oscillator is a momentum indicator used in technical analysis, 
// introduced by George Lane in the 1950s, to compare the closing price of a 
// commodity to its price range over a given time span.
using System;
using OpenTrader;

namespace OpenTrader.Indicators
{
	public class StochD : DataSeries
	{
		// Fields
		private Bars a;
		private int b;
		private int c;

		// Methods
		public StochD(Bars bars, int period, int smooth, string description) : base(bars, description)
		{
			this.a = bars;
			this.b = smooth;
			this.c = period;
			base.FirstValidValue += period + smooth;
			int num = Math.Min(bars.Count, period + smooth);
			for (int i = 0; i < num; i++)
			{
				base[i] = 50.0;
			}
			for (int j = period + smooth; j < bars.Count; j++)
			{
				double num4 = 0.0;
				double num5 = 0.0;
				for (int k = 0; k < smooth; k++)
				{
					num4 += bars.Close[j - k] - Lowest.Value(j - k, bars.Low, period);
					num5 += Highest.Value(j - k, bars.High, period) - Lowest.Value(j - k, bars.Low, period);
				}
				if (num5 != 0.0)
				{
					base[j] = 100.0 * (num4 / num5);
				}
			}
		}



		public static StochD Series(Bars bars, int period, int smooth)
		{
			string key = string.Concat(new object[] { "StochD(", period, ", ", smooth, ")" });
			if( bars.Cache.ContainsKey(key)  )
				return (StochD) bars.Cache[key];

			StochD hd = new StochD(bars, period, smooth, key);
			bars.Cache[key] = hd;
			return hd;
		}
	}
}
 
