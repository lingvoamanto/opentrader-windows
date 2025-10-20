using OpenTrader;

namespace OpenTrader.Indicators
{
	public class PeakBar : DataSeries
	{
	    // Methods
	    internal PeakBar(DataSeries A_0, string A_1) : base(A_0, A_1)
	    {
	    }
	
	    public static PeakBar Series(DataSeries source, double reversalAmount, PeakTroughMode mode)
	    {
	        PeakTroughCalculator calculator = new PeakTroughCalculator(source, reversalAmount, mode);
	        return calculator.PeakBar;
	    }
	
	    ///public static double Value(int bar, DataSeries source, double reversalAmount, PeakTroughMode mode)
	    // {
	    //    PeakTroughCalculator.a(bar, source, reversalAmount, mode);
	    //    return (double) PeakTroughCalculator.l;
	    //}
	}
}
