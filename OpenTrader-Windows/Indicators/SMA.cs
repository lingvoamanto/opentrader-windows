using System;
namespace OpenTrader.Indicators
{
    public class SMA : DataSeries
    {
        public SMA(DataSeries ds, int period, string description)
            : base(ds, description)
        {
            base.FirstValidValue = period - 1;
            for (int bar = period - 1; bar < ds.Count; bar++)
            {
                this[bar] = 0;
                for (int i = 0; i < period; i++)
                {
                    this[bar] += ds[bar - i];
                }
                this[bar] /= period;
            }
        }

        public SMA(Bars bars, double[] data, int period, string description) : base(bars, data, description)
        {
            base.FirstValidValue = period - 1;
            for (int bar = period - 1; bar < data.Length; bar++)
            {
                this[bar] = 0;
                for (int i = 0; i < period; i++)
                {
                    this[bar] += data[bar - i];
                }
                this[bar] /= period;
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

        public static SMA Series(Bars bars, double[] data, int period, string description, bool shouldCache = true)
        {
            //Build description
            description = "SMA(" + description + "," + period.ToString() + ")";

            //See if it exists in the cache
            if (bars.Cache.ContainsKey(description))
                return ((WeakReference)bars.Cache[description]).Target as SMA;

            //Create SMA, cache it, return it
            if (shouldCache)
            {
                SMA sma = new SMA(bars, data, period, description);
                bars.Cache.Add(description, new WeakReference(sma, false));
                return sma;
            }
            else
                return new SMA(bars, data, period, description);
        }

        public static SMA Series(DataSeries ds, int period, bool shouldCache = true)
        {
            //Build description
            string description = "SMA(" + ds.bars.Name + "," + ds.Description + "," + period.ToString() + ")";

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as SMA;

            //Create SMA, cache it, return it
            if (shouldCache)
            {
                SMA sma = new SMA(ds, period, description);
                ds.Cache.Add(description,new WeakReference(sma, false));
                return sma;
            }
            else
                return new SMA(ds, period, description);
        }
    }
}

