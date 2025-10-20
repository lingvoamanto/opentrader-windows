using System;
namespace OpenTrader.Indicators
{
    public class PeriodLow : DataSeries
    {
        private DataSeries ds;

        public PeriodLow(DataSeries ds, int period, string description)
            : base(ds, description)
        {
            this.ds = ds;

            int _period = Math.Min(period, ds.Count);

            for (int bar = _period - 1; bar < ds.Count; bar++)
            {
                this[bar] = ds[bar];
                for(int i=1; i<_period; i++)
                {
                    if (ds[bar - i] < this[bar])
                        this[bar] = ds[bar-i];
                }
            }
        }

        public static PeriodLow Series(DataSeries ds, int period, bool shouldCache = true)
        {
            //Build description
            string description = "PeriodLow(" + ds.bars.Name + "," + ds.Description + "," + period.ToString() + ")";

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as PeriodLow;

            //Create SMA, cache it, return it
            if (shouldCache)
            {
                PeriodLow low = new PeriodLow(ds, period, description);
                ds.Cache.Add(description, new WeakReference(low, false));
                return low;
            }
            else
                return new PeriodLow(ds, period, description);
        }
    }
}

