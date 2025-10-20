using System;
namespace OpenTrader.Data
{
    public partial class TrendLine
    {
        public bool CalculateLine(Bars bars, int firstBar, int lastBar, out double slope, out double intercept)
        {
            int startBar = 0;
            bool foundStart = false;
            for (int i = lastBar; i >= 0; i--)
            {
                if (StartDate.Date == bars.Date[i])
                {
                    foundStart = true;
                    startBar = i;
                    break;
                }
            }
            if (!foundStart)
            {
                slope = 0;
                intercept = 0;
                return false;
            }

            int endBar = 0;
            bool foundEnd = false;
            for (int i = firstBar; i < bars.Count; i++)
            {
                if (EndDate.Date == bars.Date[i])
                {
                    foundEnd = true;
                    endBar = i;
                    break;
                }
            }
            if (!foundEnd)
            {
                slope = 0;
                intercept = 0;
                return false;
            }

            slope = (EndPrice - this.StartPrice) / (endBar - startBar);
            intercept = StartPrice - slope * startBar;
            return true;
        }
    }
}
