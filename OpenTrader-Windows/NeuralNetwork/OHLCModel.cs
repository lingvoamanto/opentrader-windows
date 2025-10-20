using OpenTrader.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader.NeuralNetwork
{
    public class OHLCMOdel
    {
        public static SimpleNeuralNetwork Create()
        {
            return new NetworkBuilder(120)
                .AddRectifiedHidden(11)
                .AddRectifiedHidden(2)
                .AddSigmoidOutput()
                .Build();
        }

        public void Train(DataSeries open, DataSeries high, DataSeries low, DataSeries close)
        {
            int windowSize = 30;
            int count = close.Count - windowSize;
            var longSMA = SMA.Series(close, 399);
            var shortSMA = SMA.Series(close, 41);

            var inputVectors = new List<double[]>();
            var labels = new List<double[]>();

            for (int i = 399; i < count; i++)
            {
                double[] input = new double[windowSize * 4]; // open, high, low, close
                for (int j = 0; j < windowSize; j++)
                {
                    input[j * 4 + 0] = open[i + j];
                    input[j * 4 + 1] = high[i + j];
                    input[j * 4 + 2] = low[i + j];
                    input[j * 4 + 3] = close[i + j];
                }

                inputVectors.Add(input);

                double futureClose = close[i + windowSize];
                double entryPrice = close[i + windowSize - 1];
                double netProfit = futureClose - entryPrice; // You could subtract cost here
                double label = netProfit > entryPrice * 0.15 ? 1.0 : 0.0;

                labels.Add(new double[] { label });
            }

            var model = OHLCMOdel.Create();
            model.PushExpectedValues(labels.ToArray());
            model.Train(inputVectors.ToArray(), numberOfEpochs: count-399);
        }
    }
}
