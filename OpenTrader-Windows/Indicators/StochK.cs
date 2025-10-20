// StochK returns the Stochastic Oscillator %K. The Stochastic Oscillator measures how
// much price tends to close in the upper or lower areas of its trading range.
namespace OpenTrader.Indicators
{
	public class StochK : DataSeries
	{
		// Fields
		private Bars a;
		private int b;

		// Methods
		public StochK(Bars source, int period, string description) : base(source, description)
		{
			this.a = source;
			this.b = period;
			if ((period < 1) || (period > (source.Count + 1)))
			{
				period = source.Count + 1;
			}
			int bar = period;
			while (true)
			{
				if (bar < base.Count)
				{
					double num = Lowest.Value(bar, source.Low, period);
					double num2 = Highest.Value(bar, source.High, period);
					if ((num2 - num) == 0.0)
					{
						base[bar] = 0.0;
					}
					else
					{
						base[bar] = ((source.Close[bar] - num) / (num2 - num)) * 100.0;
					}
				}
				else
				{
					base.FirstValidValue = period;
					return;
				}
				bar++;
			}
		}

		public static StochK Series(Bars source, int period)
		{
			DataSeries series;
			string key = "StochK(" + period + ")";
			if( source.Cache.ContainsKey(key) )
				return (StochK) source.Cache[key];
			
			series = new StochK(source, period, key);
			source.Cache[key] = series;
			return (StochK) series;
		}
	}
}
 
