using System;
namespace OpenTrader.Indicators
{
    public class Stochastic : DataSeries
    {
        public Stochastic(DataSeries ds, int period, string description)
            : base(ds, description)
        {
            base.FirstValidValue = period - 1;
            for (int bar = period - 1; bar < ds.Count; bar++)
            {
                double low = this[bar];
                double high = this[bar];
                for (int i = 1; i < period; i++)
                {
                    if( ds[bar - i] < low)
                        low = ds[bar - i];
                    if(ds[bar - i] > high)
                        high = ds[bar - i];
                }
                this[bar] = 100 * (ds[bar] - low) / (high - low);
            }
        }

        public static double Value(int bar, DataSeries ds, int period)
        {
            double result = 0;
            for (int i = 0; i < period; i++)
            {
                result += ds[bar - i];
            }
            return result / period;
        }



        public static Stochastic Series(DataSeries ds, int period, bool shouldCache = true)
        {
            //Build description
            string description = "Stochastic(" + ds.bars.Name + "," + ds.Description + "," + period.ToString() + ")";

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as Stochastic;

            //Create SMA, cache it, return it
            if (shouldCache)
            {
                Stochastic sma = new Stochastic(ds, period, description);
                ds.Cache.Add(description, new WeakReference(sma, false));
                return sma;
            }
            else
                return new Stochastic(ds, period, description);
        }
    }
}

