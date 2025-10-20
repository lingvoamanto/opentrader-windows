using System;
namespace OpenTrader.Indicators
{
    public class RelativeSlope : DataSeries
    {
        public RelativeSlope(DataSeries ds, int period, string description)
            : base(ds, description)
        {
            base.FirstValidValue = Math.Max(base.FirstValidValue,period - 1);

            for (int bar = period - 1; bar < ds.Count; bar++)
            {
                this[bar] = 0;

                double sum = 0;
                for (int i = 0; i < period; i++)
                {
                    sum += ds[bar - i];
                }

                double mean = sum / period;
                double sumXY = 0;
                double sumX = 0;
                double sumY = 0;
                double sumX2 = 0;
                for (int i = 0; i < period; i++)
                {
                    double y = ds[bar - i]/mean;
                    sumXY += i * y;
                    sumX += i;
                    sumY += y;
                    sumX2 += i * i;
                }

                double slope = (period * sumXY - sumX * sumY) / (period * sumX2 - sumX * sumX);

                this[bar] /= slope;
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



        public static RelativeSlope Series(DataSeries ds, int period, bool shouldCache = true)
        {
            //Build description
            string description = "RelativeSlope(" + ds.bars.Name + "," + ds.Description + "," + period.ToString() + ")";

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as RelativeSlope;

            RelativeSlope rs = new RelativeSlope(ds, period, description);

            //Create SMA, cache it, return it
            if (shouldCache)
            {
                ds.Cache.Add(description, new WeakReference(rs, false));
            }

            return rs;
        }
    }
}

