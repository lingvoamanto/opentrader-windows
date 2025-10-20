namespace OpenTrader.Indicators
{
	public class TroughBar : DataSeries
	{
	    // Methods
	    internal TroughBar(DataSeries A_0, string A_1) : base(A_0, A_1)
	    {
	    }
	
	    public static TroughBar Series(DataSeries source, double reversalAmount, PeakTroughMode mode)
	    {
	        PeakTroughCalculator calculator = new PeakTroughCalculator(source, reversalAmount, mode);
	        return calculator.TroughBar;
	    }
	
	    // public static double Value(int bar, DataSeries source, double reversalAmount, PeakTroughMode mode)
	    // {
	    //    PeakTroughCalculator.a(bar, source, reversalAmount, mode);
	    //    return (double) PeakTroughCalculator.m;
	    // }
	}
}
 
