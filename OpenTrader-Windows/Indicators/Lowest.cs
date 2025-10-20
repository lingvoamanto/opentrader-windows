using System;
using OpenTrader;

namespace OpenTrader.Indicators
{
    public class Lowest : DataSeries
    {
        public Lowest( DataSeries ds, int period, string description )
            : base( ds, description )
        {
			int Count = ds.Count;
			for( int bar = period-1; bar < Count; bar++ )
			{
				double lowest = ds[bar];
				for( int i=1; i < period; i++ )
				{
					lowest = Math.Min( lowest, ds[bar-i] );
				}
				this[bar] = lowest;
			}
        }
		
		public static double Value( int bar, DataSeries ds, int period )
		{
			double lowest = ds[bar];
			
			for( int i=1; i < period; i++ )
				lowest = Math.Min( lowest, ds[bar-i] );

			return lowest;
		}

        public static Lowest Series( DataSeries ds, int period )
        {
            //Build description
            string description = "Lowest(" + ds.Description + "," + period.ToString() + ")" ;

            //See if it exists in the cache
            if( ds.Cache.ContainsKey( description ) )
                return (Lowest) ds.Cache[description];

            //Create Fisher, cache it, return it
            return (Lowest) (ds.Cache[description] = new Lowest( ds, period, description ));

        }
    }
}

