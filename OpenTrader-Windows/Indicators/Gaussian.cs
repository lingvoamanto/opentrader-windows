using System;
namespace OpenTrader.Indicators
{
    public class Gaussian : DataSeries
    {
        //Private members
        private DataSeries ds;
        private double c, c1, c2, c3, c4;

        //Constructor
        public Gaussian(DataSeries ds, double period, int poles, string description)
            : base(ds, description)
        {
            //Remember parameters
            this.ds = ds;

            //Assign first bar that contains indicator data
            FirstValidValue = ds.FirstValidValue + 4;
            if (FirstValidValue > ds.Count) FirstValidValue = ds.Count;

            double w = 2 * Math.PI / period; // omega
            double b = (1 - Math.Cos(w)) / (Math.Pow(2, 1.0 / poles) - 1);
            double a = -b + Math.Sqrt(b * b + 2 * b);
            double a1 = 1 - a;

            c4 = c3 = c2 = c1 = c = 0;
            switch (poles)
            {
                case 1: c1 = a1; c = a; break;
                case 2: c2 = -a1 * a1; c1 = 2 * a1; c = a * a; break;
                case 3: c3 = a1 * a1 * a1; c2 = -3 * a1 * a1; c1 = 3 * a1; c = a * a * a; break;
                case 4: c4 = -a1 * a1 * a1 * a1; c3 = 4 * a1 * a1 * a1; c2 = -6 * a1 * a1; c1 = 4 * a1; c = a * a * a * a; break;
            }
  
            //Initialize start of series
            for (int bar = 0; bar < FirstValidValue; bar++)
                this[bar] = ds[bar];

            //Rest of series
            for (int bar = FirstValidValue; bar < ds.Count; bar++)
            {
                this[bar] = c * ds[bar] + c1 * this[bar - 1] + c2 * this[bar - 2]
                                        + c3 * this[bar - 3] + c4 * this[bar - 4];
            }
        }

        //Static Series method returns an instance of the indicator
        public static Gaussian Series(DataSeries ds, double period, int poles)
        {
            //Build description
            string description = "Gaussian(" + ds.Description + "," + period + "," + poles + ")";

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return (Gaussian)ds.Cache[description];

            //Create Gaussian, cache it, return it
            return (Gaussian)(ds.Cache[description] = new Gaussian(ds, period, poles, description));
        }
/*
        //This static method allows ad-hoc calculation of Gaussian (single calc mode)
        public static double Value(int bar, DataSeries ds)
        {
        }
*/
        //Calculate a value for a partial bar

    }
}

