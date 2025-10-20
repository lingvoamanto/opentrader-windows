using System;
namespace OpenTrader
{
    public partial class DataSeries
    {
        public int[] FindPeaks()
        {
            var sample = data.ToArray();

            // Find peaks
            int[] peaks = Accord.Audio.Tools.FindPeaks(sample);
            return peaks;
        }

        public int[] FindTroughs()
        {
            var sample = data.ToArray();
            for(int i=0; i<sample.Length;i++)
            {
                sample[i] = -sample[i];
            }
            // Find peaks
            int[] troughs = Accord.Audio.Tools.FindPeaks(sample);
            return troughs;
        }
    }
}
