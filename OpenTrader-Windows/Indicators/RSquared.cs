namespace OpenTrader.Indicators
{
	public class RSquared : DataSeries
	{
	    // Fields
	    private DataSeries a;
	    private int b;
	
	    // Methods
	    public RSquared(DataSeries source, int period, string description) : base(source, description)
	    {
	        this.a = source;
	        this.b = period;
	        base.FirstValidValue = period;
	        WMA wma = WMA.Series(source, period);
	        SMA sma = SMA.Series(source, period);
	        StdDev dev = StdDev.Series(source, period, StdDevCalculation.Population);
	        DataSeries series = wma - sma;
	        DataSeries series2 = series / dev;
	        DataSeries series3 = series2 * series2;
	        for (int i = base.FirstValidValue; i < source.Count; i++)
	        {
	            if (dev[i] == 0.0)
	            {
	                base[i] = 1.0;
	            }
	            else
	            {
	                base[i] = ((3.0 * (period + 1.0)) / (period - 1.0)) * series3[i];
	            }
	        }
	    }
	
	
	    public static RSquared Series(DataSeries source, int period)
	    {
	        string key = string.Concat(new object[] { "RSquared(", source.Description, ", ", period, ")" });
	        if( source.Cache.ContainsKey(key) )
	        	return (RSquared) source.Cache[key];
			
	        RSquared squared = new RSquared(source, period, key);
	        source.Cache[key] = squared;
	        return squared;
	    }
	
	    public static double Value(int bar, DataSeries source, int period)
	    {
	        WMA wma = WMA.Series(source, period);
	        SMA sma = SMA.Series(source, period);
	        StdDev dev = StdDev.Series(source, period, StdDevCalculation.Population);
	        DataSeries series = wma - sma;
	        DataSeries series2 = series / dev;
	        DataSeries series3 = series2 * series2;
	        if (((dev[bar] == 0.0) ? 0 : 1) != 0)
	        {
	        }
	        return (((3.0 * (period + 1.0)) / (period - 1.0)) * series3[bar]);
	    }
	}
}

 