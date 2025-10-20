using System;
namespace OpenTrader.Indicators
{
    public enum StdDevCalculation { Population = 0, Sample = 1 }

    public class StdDev : DataSeries
    {
        private DataSeries ds;

        public StdDev(DataSeries ds, int period, OpenTrader.Indicators.StdDevCalculation calcType, string description)
            : base(ds, description)
        {
            this.ds = ds;

            DataSeries mean = SMA.Series(ds, period);

            for (int bar = period - 1; bar < ds.Count; bar++)
            {
                double sumsquares = 0;
                for (int i = 0; i < period; i++)
                {
                    double distance = ds[bar - i] - mean[bar];
                    sumsquares += distance * distance;
                }

                this[bar] = Math.Sqrt(sumsquares / (period - (int)calcType));
            }
        }

        public static StdDev Series(DataSeries ds, int Period, StdDevCalculation calcType = StdDevCalculation.Sample)
        {
            //Build description
            string description = "StdDev(" + ds.bars.Name + "," + ds.Description + "," + Period.ToString() + "," + calcType.ToString() + ")";

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ds.Cache[description].Target as StdDev;
            else
            {
                //Create Band, cache it, return it
                StdDev stdDev = new StdDev(ds, Period, calcType, description);
                ds.Cache.Add(description,new WeakReference(stdDev, false));
                return stdDev;
            }
        }
    }

}

