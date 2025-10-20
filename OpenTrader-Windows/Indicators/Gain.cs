using System;
using OpenTrader;

namespace OpenTrader.Indicators
{
    public class Gain : DataSeries
    {
        private DataSeries ds;

        public Gain( DataSeries ds, string description )
            : base( ds, description )
        {
            this.ds = ds;
            for( int bar = 1; bar < ds.Count; bar++ )
            {
                double diff = ds[bar] - ds[bar - 1];
                if( diff > 0 )
                    this[bar] = diff;
                else
                    this[bar] = 0;
            }
        }

        public static Gain Series( DataSeries ds )
        {
            //Build description
            string description = "Gain(" + ds.Description + ")";

            //See if it exists in the cache
            if( ds.Cache.ContainsKey( description ) )
                return (Gain) ds.Cache[description];

            //Create Fisher, cache it, return it
            return (Gain) (ds.Cache[description] = new Gain( ds, description ));

        }
    }
}

