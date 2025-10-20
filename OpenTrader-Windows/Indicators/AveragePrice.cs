using System;
namespace OpenTrader.Indicators
{
	public class AveragePrice : DataSeries
	{
		public AveragePrice( Bars bars, string description ) : base( bars, description )
		{
			for( int bar =0; bar< bars.Count; bar++ )
			{
				this[bar] = (bars.High[bar]+bars.Low[bar]) / 2.0;
			}
		}
		
		public static AveragePrice Series( Bars bars )
		{
            //Build description
            string description = "AveragePrice";

            //See if it exists in the cache
            if( bars.Cache.ContainsKey( description ) )
                return (AveragePrice) bars.Cache[description];

            //Create Fisher, cache it, return it
            return (AveragePrice) (bars.Cache[description] = new AveragePrice( bars, description ));			
		}
	}
}

