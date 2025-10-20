using System;
using OpenTrader;

namespace OpenTrader.Indicators
{
	public class Peak : DataSeries
	{
	    // Methods
	    internal Peak(DataSeries source, string key) : base( source, key )
	    {
	    }
	
	    public static Peak Series(DataSeries source, double reversalAmount, PeakTroughMode mode)
	    {
	        PeakTroughCalculator calculator = new PeakTroughCalculator(source, reversalAmount, mode);
	        return calculator.Peak;
	    }
	
	    // public static double Value(int bar, DataSeries source, double reversalAmount, PeakTroughMode mode)
	    // {
	    //     PeakTroughCalculator.a(bar, source, reversalAmount, mode);
	    //    return PeakTroughCalculator.j;
	    // }
	}
}

 
