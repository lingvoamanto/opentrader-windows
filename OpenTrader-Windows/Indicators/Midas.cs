using System;
namespace OpenTrader.Indicators
{
    public class Midas : DataSeries
    {
        //Private members
        private Bars ds;
        private double _cumPV = 0;
        private double _cumV = 0;  
        private double _cumVst = 0;
        private double _cumPVst = 0; 

        //Constructor
        public Midas(Bars ds, int startBar, string description)
            : base(ds, description)
        {
            //Remember parameters
            this.ds = ds;            

            //Assign first bar that contains indicator data
            FirstValidValue = startBar;
            if (FirstValidValue > ds.Count || FirstValidValue < 0)
            {
                FirstValidValue = ds.Count;
                return;
            }

            //Initialization before first value
            DataSeries ap = AveragePrice.Series(ds);
          
            for (int bar = 0; bar <= FirstValidValue; bar++)
            {
                this[bar] = ap[bar];
                _cumV += ds.Volume[bar];
                _cumPV += ap[bar] * ds.Volume[bar];
            }            
            _cumVst = _cumV;
            _cumPVst = _cumPV;                               
           
            for (int bar = FirstValidValue + 1; bar < ds.Count; bar++)
            {
                _cumV += ds.Volume[bar];
                _cumPV += ap[bar] * ds.Volume[bar];
                double dV = _cumV - _cumVst;
                dV = dV == 0 ? 1 : dV;
                this[bar] = (_cumPV - _cumPVst) / dV;
            }
        }

        //Static Series method returns an instance of the indicator
        public static Midas Series(Bars ds, int startBar)
        {
            //Build description
            string description = string.Concat("Midas(", startBar, ")");

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return (Midas)ds.Cache[description];

            //Create Midas, cache it, return it
            return (Midas)(ds.Cache[description] = new Midas(ds, startBar, description));
        }
    }
}

