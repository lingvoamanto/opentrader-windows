using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Media.Imaging;

namespace OpenTrader
{
    public static partial class Extensions
    {
        public static double[] SmaSeries(this double[] data, int period)
        {
            double[] series  = new double[data.Length];

            for (int bar = period - 1; bar < data.Length; bar++)
            {
                series[bar] = 0;
                for (int i = 0; i < period; i++)
                {
                    series[bar] += data[bar - i];
                }
                series[bar] /= period;
            }

            return series;
        }

        public static double[] EmaSeries(this double[] data, int period, Indicators.EMACalculation calcType = Indicators.EMACalculation.Modern)
        {
            var sma = data.SmaSeries(period);
            var series = new double[data.Length];
            double c;

            if (period < data.Length)
            {
                double num = sma[period - 1];
                series[period - 1] = num;
                if (calcType != Indicators.EMACalculation.Modern)
                {
                    c = 2.0 / period;
                }
                else
                {
                    c = 2.0 / (1.0 + period);
                }

                for (int i = period; i < data.Length; i++)
                {
                    double num2 = data[i] - num;
                    num2 *= c;
                    num += num2;
                    series[i] = num;
                }
            }
            return series;
        }
    }
}