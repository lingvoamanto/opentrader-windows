// Divergence returns the divergence of a DataSeries from a selected Moving Average.  
// This is usualy used in conjunction with the MACD indicator to show the divergence 
// between the MACD line and the MACD Signal line (EMA with a period of 9).
namespace OpenTrader.Indicators
{
	public class Divergence : DataSeries
	{
		// Fields
		private DataSeries source;
		private DataSeries MA;
		private int period;
		private MAType maType;

		// Methods
		public Divergence(DataSeries source, MAType maType, int period, string description) : base(source, description)
		{
			this.source = source;
			this.period = period;
			base.FirstValidValue = (period - 1) + source.FirstValidValue;
			this.maType = maType;
			switch (maType)
			{
				case MAType.SMA:
					MA = SMA.Series(source, period);
					break;

				case MAType.EMAModern:
					MA = EMAModern.Series(source, period);
					break;

				case MAType.EMALegacy:
					MA = EMALegacy.Series(source, period);
					break;

				case MAType.WMA:
					MA = WMA.Series(source, period);
					break;

				case MAType.VMA:
					MA = VMA.Series(source, period);
					break;
			}
			
			for( int bar = period; bar < source.Count; bar++ )
			{
				this[bar] = source[bar] - MA[bar];
			}
		}
		
		// Leave this till we figure out what to do with it

		/*
		public override void CalculatePartialValue()
		{
			int num = 4;
		Label_000D:
			switch (num)
			{
				case 0:
					if (this.b.PartialValue != double.NaN)
					{
						base.PartialValue = this.a.PartialValue - this.b.PartialValue;
						return;
					}
					num = 5;
					goto Label_000D;

				case 1:
					if (this.a.PartialValue == double.NaN)
					{
						break;
					}
					num = 2;
					goto Label_000D;

				case 2:
					num = 0;
					goto Label_000D;

				case 3:
					num = 1;
					goto Label_000D;

				case 5:
					break;

				default:
					if (((this.a.Count < this.c) ? 0 : 1) != 0)
					{
					}
					num = 3;
					goto Label_000D;
			}
			base.PartialValue = double.NaN;
		}
		*/

		public static Divergence Series(DataSeries source, MAType maType, int period)
		{
			string key = string.Concat(new object[] { "Divergence(", source.Description, ",", maType.ToString(), ",", period, ")" });
			if( source.Cache.ContainsKey(key) )
				return (Divergence) source.Cache[key];
			
			Divergence divergence = new Divergence(source, maType, period, key);
			source.Cache[key] = divergence;
			return divergence;
		}
	}

 }

 
