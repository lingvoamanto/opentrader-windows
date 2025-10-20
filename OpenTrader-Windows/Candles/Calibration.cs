using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTrader;

namespace OpenTrader
{
    public enum MarketType { All, Bear, Bull}
}

namespace OpenTrader
{
    public class Calibration
    {
        Data.DataSet dataSet;
        List<Data.CandleData> profits;
        public string[] names;
        Data.DataFile? indexFile = null;

        double[]? smaSlow;
        double[]? smaFast;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
// The compiler is wrong
        public Calibration(OpenTrader.Data.DataSet dataSet)

        {
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            this.dataSet = dataSet;
            profits = Data.CandleData.GetDataSet(dataSet.Guid);
            foreach(var profit in profits)
            {
                profit.N = 0;
                profit.Sum = 0;
                profit.SumSquared = 0;
                profit.PositiveN = 0;
                profit.PositiveSum = 0;
                profit.PositiveSumSquared = 0;
                profit.NegativeN = 0;
                profit.NegativeSum = 0;
                profit.NegativeSumSquared = 0;
            }

            var yahooIndex = dataSet.YahooIndex;

            foreach (var dataFile in dataSet.DataFiles)
            {
                if (dataFile.YahooCode == yahooIndex)
                {
                    indexFile = dataFile;
                    break;
                }
            }

            if (indexFile == null)
            {
                return;
            }

            var indexClose = indexFile.bars.Close.ToArray();

            smaFast = indexClose.SmaSeries(41);
            smaSlow = indexClose.SmaSeries(399);
        }

        void AddProfit(MarketType marketType, string name, double x)
        {
            var index = profits.FindIndex(p => p.Name == name && p.MarketType == marketType);

            var xSquared = x * x;

            if (index < 0)
            {
                var profit = new Data.CandleData()
                {
                    Name = name,
                    N = 1,
                    Sum = x,
                    SumSquared = xSquared,
                    MarketType = marketType
                };
                if (x == 0)
                {
                    profit.PositiveN = 0;
                    profit.PositiveSum = 0;
                    profit.PositiveSumSquared = 0;
                    profit.NegativeN = 0;
                    profit.NegativeSum = 0;
                    profit.NegativeSumSquared = 0;
                }
                else if (x > 0)
                {
                    profit.PositiveN = 1;
                    profit.PositiveSum = x;
                    profit.PositiveSumSquared = xSquared;
                    profit.NegativeN = 0;
                    profit.NegativeSum = 0;
                    profit.NegativeSumSquared = 0;
                }
                else
                {
                    profit.NegativeN = 1;
                    profit.NegativeSum = -x;
                    profit.NegativeSumSquared = xSquared;
                    profit.PositiveN = 1;
                    profit.PositiveSum = x;
                    profit.PositiveSumSquared = xSquared;
                }
                profits.Add(profit);
            }
            else
            {
                var profit = profits[index];
                profit.N++;
                profit.Sum += x;
                profit.SumSquared += xSquared;

                if (x > 0)
                {
                    profit.PositiveN++;
                    profit.PositiveSum += x;
                    profit.PositiveSumSquared += xSquared;
                }
                else if (x < 0)
                {
                    profit.NegativeN++;
                    profit.NegativeSum -= x;
                    profit.NegativeSumSquared += xSquared;
                }
            }
        }

        public double GetProfit(Bars bars, int bar)
        {
            return (bars.Close[bar + 10] - bars.Close[bar]) / bars.Close[bar];
        }

        public void Calibrate()
        {
            Indicators.Candles candles;

            int nCalibrated = 0;

            // Go through each 
            //Parallel.ForEach(dataSet.DataFiles, dataFile =>
            foreach (var dataFile in dataSet.DataFiles)
           {
               var bars = dataFile.bars;
               var count = bars.Count;
               if (count == 0)
               {
                    continue;
               }
               nCalibrated++;

               candles = new Indicators.Candles(bars);
               names = candles.GetNames();
               var list = candles.FindNames(names);
               foreach (var item in list)
               {
                   int bar = item.bar;
                   if (bar + 10 < bars.Close.Count && bars.Close[bar] > 0)
                   {

                       var x = GetProfit(bars, bar);
                       AddProfit(MarketType.All, item.name, x);

                        if (indexFile!= null && smaSlow != null & smaFast != null)
                        {
                            var indexBar = indexFile.bars.Date.FindIndex( d=>d.Date == dataFile.bars.Date[bar].Date);
                            if (indexBar >= 0)
                            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
// smaFast is not null here. Stupid compiler.
                                if (smaFast[indexBar] < smaSlow[indexBar])
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                                    AddProfit(MarketType.Bear, item.name, x);
                                if (smaFast[indexBar] > smaSlow[indexBar])
                                    AddProfit(MarketType.Bull, item.name, x);
                            }
                        }
                    }
               }
            } //);

            // Now save the data
            foreach(var profit in profits )
            {
                profit.DataSetGuid = dataSet.Guid;
                profit.Save();
            }
        }
    }
}
