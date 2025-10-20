using System;

namespace OpenTrader.Indicators
{
    // EMA returns the Exponential Moving Average.  This is a way of averaging a set of values
    // but giving more weight to the data that occurs more recently.
    public enum MAType
    {
        SMA = 0, EMAModern = 1, EMALegacy = 2, WMA = 3, VMA = 4
    }

    public enum EMACalculation
    {
        Modern = 0, Legacy = 1
    }


    public class EMA : DataSeries
    {
        private DataSeries a;
        private int b;
        private double c;

        public EMA(DataSeries source, int period, EMACalculation calcType, string description) : base(source, description)
        {
            this.a = source;
            this.b = period;
            base.FirstValidValue = (period - 1) + source.FirstValidValue;
            if (period < base.Count)
            {
                double num = SMA.Value(period - 1, source, period);
                base[period - 1] = num;
                if (calcType != EMACalculation.Modern)
                {
                    this.c = (1.0 / ((double)period)) * 2.0;
                }
                else
                {
                    this.c = 2.0 / (1.0 + period);
                }
                for (int i = period; i < source.Count; i++)
                {
                    double num2 = source[i] - num;
                    num2 *= this.c;
                    num += num2;
                    base[i] = num;
                }
            }
        }

        public static EMA Series(DataSeries source, int period, EMACalculation calcType=EMACalculation.Modern, bool shouldCache=true)
        {
            string key = string.Concat(new object[] { "EMA(" + source.bars.Name +","+source.Description, ",", period, ",", calcType, ")" });

            //See if it exists in the cache
            if (source.Cache.ContainsKey(key))
                return ((WeakReference)source.Cache[key]).Target as EMA;

            EMA ema = new EMA(source, period, calcType, key);

            //Create SMA, cache it, return it
            if (shouldCache)
            {
                source.Cache.Add(key, new WeakReference(ema, false));
            }

            return ema;
        }
    }

    public static class EMALegacy
    {
        // Methods
        public static DataSeries Series(DataSeries source, int period)
        {
            return EMA.Series(source, period, EMACalculation.Legacy);
        }
    }

    public static class EMAModern
    {
        // Methods
        public static DataSeries Series(DataSeries source, int period)
        {
            return EMA.Series(source, period, EMACalculation.Modern);
        }
    }
}




