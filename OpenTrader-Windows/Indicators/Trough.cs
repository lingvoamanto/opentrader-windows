namespace OpenTrader.Indicators
{
	public class Trough : DataSeries
	{
	    // Methods
	    internal Trough(DataSeries source, string key) : base( source, key)
	    {
	    }
	
	    public static Trough Series(DataSeries source, double reversalAmount, PeakTroughMode mode)
	    {
	        PeakTroughCalculator calculator = new PeakTroughCalculator(source, reversalAmount, mode);
	        return calculator.Trough;
	    }
	
	    // public static double Value(int bar, DataSeries source, double reversalAmount, PeakTroughMode mode)
	    // {
	    //    PeakTroughCalculator.a(bar, source, reversalAmount, mode);
	    //    return PeakTroughCalculator.k;
	    // }
	}
}

 
