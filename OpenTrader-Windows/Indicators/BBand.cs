using System;
namespace OpenTrader.Indicators
{
    public class BBandLower : DataSeries
    {
        private DataSeries ds;

        public BBandLower(DataSeries ds, int period, double stdDevs, string description)
            : base(ds, description)
        {
            this.ds = ds;

            DataSeries Sma = SMA.Series(ds, period);
            DataSeries SDev = StdDev.Series(ds, period, StdDevCalculation.Population);

            for (int bar = period - 1; bar < ds.Count; bar++)
            {
                this[bar] = Sma[bar] - stdDevs * SDev[bar];
            }
        }

        public static BBandLower Series(DataSeries ds, int Period, double stdDevs)
        {
            //Build description
            string description = "BBandLower(" + ds.bars.Name + "," + ds.Description + "," + Period.ToString() + "," + stdDevs.ToString() + ")";

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ds.Cache[description].Target as BBandLower;
            else
            {
                //Create Band, cache it, return it
                BBandLower lower = new BBandLower(ds, Period, stdDevs, description);
                ds.Cache.Add(description,new WeakReference(lower,false));
                return lower;
            }
        }
    }

    public class BBandUpper : DataSeries
    {
        private DataSeries ds;

        public BBandUpper(DataSeries ds, int period, double stdDevs, string description)
            : base(ds, description)
        {
            this.ds = ds;

            DataSeries Sma = SMA.Series(ds, period);
            DataSeries SDev = StdDev.Series(ds, period, StdDevCalculation.Population);

            for (int bar = period - 1; bar < ds.Count; bar++)
            {
                this[bar] = Sma[bar] + stdDevs * SDev[bar];
            }
        }

        public static BBandUpper Series(DataSeries ds, int Period, double stdDevs)
        {
            //Build description
            string description = "BBandUpper(" + ds.Description + "," + Period.ToString() + "," + stdDevs.ToString() + ")";

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ds.Cache[description].Target as BBandUpper;
            else
            {
                //Create Band, cache it, return it
                BBandUpper upper = new BBandUpper(ds, Period, stdDevs, description);
                ds.Cache.Add(description,new WeakReference(upper, false));
                return upper;
            }

        }
    }
}

