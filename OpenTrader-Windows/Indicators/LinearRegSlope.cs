using System;
namespace OpenTrader.Indicators
{
	public class LinearRegSlope : DataSeries
	{
		private DataSeries ds;
		
        public LinearRegSlope( DataSeries ds, int period, string description )
            : base( ds, description )
        {
            this.ds = ds;

			for( int bar=period-1; bar < ds.Count; bar++ )
			{
				double xAvg = 0;
		        double yAvg = 0;
		
		        for (int x = 0; x < period; x++)
		        {
		            xAvg += x;
		            yAvg += ds[bar-x];
		        }
		
		        xAvg = xAvg / period;
		        yAvg = yAvg / period;
		
		        double v1 = 0;
		        double v2 = 0;
		
		        for (int x = 0; x < period; x++)
		        {
		            v1 += (x - xAvg) * (ds[bar-x] - yAvg);
		            v2 += Math.Pow(x - xAvg, 2);
		        }
				
		        this[bar] = -v1 / v2;  // slope
		        // double b = yAvg - a * xAvg;		// intercept
			}
            
        }
		
		public static LinearRegSlope Series( DataSeries ds, int Period )
        {
            //Build description
            string description = "LinearRegSlope(" + ds.Description + "," + Period.ToString() + ")";

            //See if it exists in the cache
            if( ds.Cache.ContainsKey( description ) )
                return (LinearRegSlope) ds.Cache[description];

            //Create Band, cache it, return it
            return (LinearRegSlope) (ds.Cache[description] = new LinearRegSlope( ds, Period, description ));

        }
	}
}

