using System;
using OpenTrader;

namespace OpenTrader.Indicators
{
    public class Loss : DataSeries
    {
        private DataSeries ds;

        public Loss( DataSeries ds, string description )
            : base( ds, description )
        {
            this.ds = ds;
            for( int bar = 1; bar < ds.Count; bar++ )
            {
                double diff = ds[bar - 1] - ds[bar];
                if( diff > 0 )
                    this[bar] = diff;
                else
                    this[bar] = 0;
            }
        }

        public static Loss Series( DataSeries ds )
        {
            //Build description
            string description = "Loss(" + ds.Description + ")";

            //See if it exists in the cache
            if( ds.Cache.ContainsKey( description ) )
                return (Loss) ds.Cache[description];

            //Create Fisher, cache it, return it
            return (Loss) (ds.Cache[description] = new Loss( ds, description ));

        }
    }
}

