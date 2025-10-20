using System;
namespace OpenTrader.Indicators
{
    public class Value : DataSeries
    {
        private DataSeries ds;

        public Value(DataSeries ds, double value, string description)
            : base(ds, description)
        {
            this.ds = ds;

            for (int bar = 0; bar < ds.Count; bar++)
            {
                this[bar] = value;
            }
        }

        public static Value Series(DataSeries ds, double value, bool shouldCache = true)
        {
            //Build description
            string description = "Value(" + ds.bars.Name + "," + ds.Description + "," + value.ToString() + ")";

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as Value;

            //Create SMA, cache it, return it
            if (shouldCache)
            {
                Value valueSeries = new Value(ds, value, description);
                ds.Cache.Add(description, new WeakReference(valueSeries, false));
                return valueSeries;
            }
            else
                return new Value(ds, value, description);
        }
    }
}

