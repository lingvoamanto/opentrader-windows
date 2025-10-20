using OpenTrader;

namespace OpenTrader.Indicators
{
	public enum PeakTroughMode
	{
	    Value=0,
	    Percent=1
	}
	
	public class PeakTroughCalculator
	{
	    // Fields
	    private PeakTroughMode mode;  //a
	    private DataSeries source;  //b
	    private double lowest; //c
	    private double highest;
	    private double reversalamount; //e
	    private Peak peaks; //f
	    private Trough troughs;
	    private PeakBar peakbars;
	    private TroughBar troughbars;
	    // internal static double j;
	    // internal static double k;
	    // internal static int l;
	    // internal static int m;
	    // internal static PeakTroughMode n;
	    // internal static DataSeries o;
	    // internal static double p;
	    // internal static double q;
	    // internal static double r;
	
	    // Methods
	    public PeakTroughCalculator(DataSeries source, double reversalAmount, PeakTroughMode mode)
	    {
			this.mode = mode;
            this.source = source;
            this.reversalamount = reversalAmount;
			
	        bool rising;
	        bool dropping;
	        double peak;
	        double trough;
	        int peakbar;
	        int troughbar;
	        int highestbar;
	        int lowestbar; 
			
			if( source.Count == 0 )
				return;
			
	        string peakkey = "Peak(" + source.Description + "," + reversalAmount + "," + mode + ")";
			string troughkey = "Trough(" + source.Description + "," + reversalAmount + "," + mode + ")";
			string peakbarkey =  "PeakBar(" + source.Description + "," + reversalAmount + "," + mode + ")";
			string troughbarkey = "TroughBar(" + source.Description + "," + reversalAmount + "," + mode + ")";
	        if( source.Cache.ContainsKey(peakkey) )
			{
	            this.peaks = (Peak) source.Cache[peakkey];
	            this.troughs = (Trough) source.Cache[troughkey];
	            this.peakbars = (PeakBar) source.Cache[peakbarkey];
	            this.troughbars = (TroughBar) source.Cache[troughbarkey];
	            return;				
			}

            this.peaks = new Peak( source, peakkey);
            source.Cache.Add( peakkey, this.peaks );
            this.troughs = new Trough(source, troughkey );
            source.Cache.Add( troughkey, this.troughs );
            this.peakbars = new PeakBar(source, peakbarkey);
            source.Cache.Add(peakbarkey, this.peakbars);
            this.troughbars = new TroughBar(source, troughbarkey );
            source.Cache.Add(troughbarkey, this.troughbars);


            rising = dropping = true;
            peak = trough = lowest = highest = source[0];
            peakbar = troughbar = highestbar = lowestbar = -1;
			
	        for( int bar=0; bar < source.Count; bar++ )
	        {
                if( rising )
                {
                    if (source[bar] > highest)
                    {
						// record the new high
                        highest = source[bar];
                        highestbar = bar;
                    }
                    if( this.turneddown( bar ) )
                    {
						// we're now dropping
                        rising = false;
                        dropping = true;
						// remember the last high as the last peak
                        peak = highest;
                        peakbar = highestbar;
						// start a new low
						lowest = source[bar];
                        lowestbar = bar;
                    }
                }
                if( dropping )
                {
                    if (source[bar] < lowest)
                    {
						// record the new low
                        lowest = source[bar];
                        lowestbar = bar;
                    }
                    if( this.turnedup( bar ) )
                    {
						// we're now rising
                        dropping = false;
                        rising = true;
						// remember the last low as the last peak
						trough = lowest;    
						troughbar = lowestbar;
						// start a new high
						highest = source[bar];
                        highestbar = bar;
					}
                }

	            this.peaks[bar] = peak;
	            this.troughs[bar] = trough;
	            this.peakbars[bar] = peakbar;
	            this.troughbars[bar] = troughbar;
	        }
	    }
	
		
		public bool turnedup( int bar )
		{
			if( this.mode == PeakTroughMode.Percent )
				return (this.source[bar] - this.lowest)*100 / this.lowest > this.reversalamount;
			else
				return (this.source[bar] - this.lowest) > this.reversalamount;
		}
		
		public bool turneddown( int bar )
		{
			if( this.mode == PeakTroughMode.Percent )
				return (this.highest - this.source[bar])*100 / this.highest > this.reversalamount;
			else
				return (this.highest - this.source[bar]) > this.reversalamount;
		}
	
	    // Properties
	    public Peak Peak
	    {
	        get { return this.peaks; }
	    }
	
	    public PeakBar PeakBar
	    {
	        get { return this.peakbars; }
	    }
	
	    public Trough Trough
	    {
	        get { return this.troughs; }
	    }
	
	    public TroughBar TroughBar
	    {
	        get { return this.troughbars; }
	    }
	}
}

