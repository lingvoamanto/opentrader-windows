using System;
using OpenTrader;

namespace OpenTrader.Indicators
{
   public class BourseRsi : DataSeries
    {
        private DataSeries ds;

        public BourseRsi( DataSeries ds, int period, string description )
            : base( ds, description )
        {
            this.ds = ds;

            DataSeries GainSeries = Gain.Series( ds );
            DataSeries LossSeries = Loss.Series( ds );

            double LossSum = 0;
            double GainSum = 0;

            for( int bar = 1; bar < period; bar++ )
            {
                GainSum += GainSeries[bar] ;
                LossSum += LossSeries[bar] ;
            }

            for( int bar = period; bar < ds.Count; bar++ )
            {
                GainSum += GainSeries[bar];
                LossSum += LossSeries[bar];

                if( LossSum == 0 )
                    this[bar] = 100;
                else
                    this[bar] = 100 - 100 / (1 + GainSum / LossSum);

                GainSum -= GainSeries[bar - period + 1];
                LossSum -= LossSeries[bar - period + 1];
            }
        }

        public BourseRsi( DataSeries ds, DataSeries Period, string description )
            : base( ds, description )
        {
            this.ds = ds;

            DataSeries GainSeries = Gain.Series( ds );
            DataSeries LossSeries = Loss.Series( ds );

            for( int bar = 1; bar < ds.Count; bar++ )
            {
                if( Period[bar] > 0 && Period[bar] < bar )
                {
                    double LossSum = 0;
                    double GainSum = 0;

                    for( int i = bar - (int) Math.Floor(Period[bar]) + 1; i <= bar; i++ )
                    {
                        GainSum += GainSeries[i];
                        LossSum += LossSeries[i];
                    }

                    if( LossSum == 0 )
                        this[bar] = 100;
                    else
                        this[bar] = 100 - 100 / (1 + GainSum / LossSum);
                }
            }
        }

        public static BourseRsi Series( DataSeries ds, int period )
        {
            //Build description
            string description = "BourseRsi(" + ds.Description + "," + period.ToString() + ")";

            //See if it exists in the cache
            if( ds.Cache.ContainsKey( description ) )
                return (BourseRsi) ds.Cache[description];

            //Create Fisher, cache it, return it
            return (BourseRsi) (ds.Cache[description] = new BourseRsi( ds, period, description ));

        }

        public static BourseRsi Series( DataSeries ds, DataSeries Period )
        {
            //Build description
            string description = "BourseRsi(" + ds.Description + "," + Period.Description + ")";

            //See if it exists in the cache
            if( ds.Cache.ContainsKey( description ) )
                return (BourseRsi) ds.Cache[description];

            //Create Fisher, cache it, return it
            return (BourseRsi) (ds.Cache[description] = new BourseRsi( ds, Period, description ));

        }
    }
}
