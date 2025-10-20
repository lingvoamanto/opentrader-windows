using System;
using OpenTrader;

namespace OpenTrader.Indicators
{
    public class Highest : DataSeries
    {
        public Highest( DataSeries ds, int period, string description )
            : base( ds, description )
        {
			int Count = ds.Count;
			for( int bar = period-1; bar < Count; bar++ )
			{
				double highest = ds[bar];
				for( int i=1; i < period; i++ )
				{
					highest = Math.Max( highest, ds[bar-i] );
				}
				this[bar] = highest;
			}
        }
		
		public static double Value( int bar, DataSeries ds, int period )
		{
			double highest = ds[bar];
			
			for( int i=1; i < period; i++ )
				highest = Math.Max( highest, ds[bar-i] );

			return highest;
		}

        public static Highest Series( DataSeries ds, int period )
        {
            //Build description
            string description = "Highest(" + ds.Description + "," + period.ToString() + ")";

            //See if it exists in the cache
            if( ds.Cache.ContainsKey( description ) )
                return (Highest) ds.Cache[description];

            //Create Fisher, cache it, return it
            return (Highest) (ds.Cache[description] = new Highest( ds, period, description ));

        }
    }
}

