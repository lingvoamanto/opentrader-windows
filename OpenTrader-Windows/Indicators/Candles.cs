using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;
#if __WINDOWS__
using ILGPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime;
using ILGPU.Algorithms;
#endif

namespace OpenTrader.Indicators
{
    public class Candles
    {
        #region Fields
        double[] high, low, close, open, body;
        double[] size;
        double[] upperShadow;
        double[] lowerShadow;
        double[] smaBodyLong;
        double[] smaBodyShort;
        double[] smaBodyVeryShort;
        double[]? ema, sma;
        DateTime[] date;
        int count;
        string name;
        int smaPeriod, emaPeriod;

        CandleSetting[] candleSettings;

        delegate List<(string, int)> CandleMethod(int startIdx);

        (CandleMethod method, string name)[] candleMethods;
        #endregion Fields

        #region Constructors
        /// <summary>
        /// Constructor for Candles that enables overriding of specific simple moving averages.
        /// </summary>
        /// <param name="bars">bars contains high,low, close, volume</param>
        /// <param name="candleSettings">override any default settings</param>
        /// <param name="smaPeriod">the SMA period to look for a trend</param>
        /// <param name="emaPeriod">the EMA period to look for a trend</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Candles(Bars bars, CandleSetting[] candleSettings, int smaPeriod = 3, int emaPeriod = 9)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            // Get the defaults for the SMA period and multiplication factor 
            this.candleSettings = CandleSetting.Defaults;

            // Override with any user values
            if (candleSettings != null)
            {
                foreach (var candleSetting in candleSettings)
                {
                    this.candleSettings[(int)candleSetting.CandleSettingType] = candleSetting;
                }
            }

            InitialiseAverages(bars, smaPeriod, emaPeriod);
            InitialiseMethods();
        }

        /// <summary>
        /// Simple constructor for Candles.
        /// </summary>
        /// <param name="bars">bars contains high,low, close, volume</param>
        /// <param name="smaPeriod">the SMA period to look for a trend</param>
        /// <param name="emaPeriod">the EMA period to look for a trend</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Candles(Bars bars, int smaPeriod = 3, int emaPeriod = 9)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            // Get the defaults for the SMA period and multiplication factor 
            this.candleSettings = CandleSetting.Defaults;

            InitialiseAverages(bars, smaPeriod, emaPeriod);
            InitialiseMethods();
        }

#if __WINDOWS__
        static void CandleKernel(
            Index1 i, // The global thread index (1D in this case)
ArrayView<double> open,
ArrayView<double> high,
ArrayView<double> low,
ArrayView<double> close,
ArrayView<double> body,
ArrayView<double> size,
ArrayView<double> upper,
ArrayView<double> lower
            )
        {
            body[i] = Math.Abs(open[i] - close[i]);
            size[i] = high[i] - low[i];
            upper[i] = high[i] - Math.Max(open[i], close[i]);
            lower[i] = Math.Min(open[i], close[i]) - low[i];
        }


        static void SmaKernel(
Index1 index, // The global thread index (1D in this case)
ArrayView<double> dataView,
ArrayView<double> smaView,
int period
    )
        {
            var start = Math.Max(index - period + 1, 0);
            var count = 0;
            double sum = 0;
            for (int i = start; i <= index; i++)
            {
                sum += dataView[i];
                count++;
            }

            smaView[index] = sum / count;
        }

        static void EmaKernel( ArrayView<double> data, ArrayView<double> series, int calcType, int period )
        {
            double c;
            double[] sma = new double[period];

            if (period < data.Length)
            {
                if (calcType != 0 ) // 0 is modern
                {
                    c = 2.0 / period;
                }
                else
                {
                    c = 2.0 / (1.0 + period);
                }

                sma[0] = data[0];
                for( int i=1; i<period; i++)
                {
                    sma[i] = (sma[i - 1] * i + data[i]) / (i + 1);
                }

                // TODO: put in sma calculation
                
                double num = sma[period - 1];
                for (int i = period; i < data.Length; i++)
                {
                    double num2 = data[i] - num;
                    num2 *= c;
                    num += num2;
                    series[i] = num;
                }
            }
        }


        void InitialiseAveragesParallel(Bars bars, int smaPeriod, int emaPeriod)
        {
            this.smaPeriod = smaPeriod;
            this.emaPeriod = emaPeriod;

            open = bars.Open.ToArray();
            high = bars.High.ToArray();
            low = bars.Low.ToArray();
            close = bars.Close.ToArray();

            // Create the required ILGPU context
            using var context = new Context();
            using var accelerator = new CudaAccelerator(context);

            // accelerator.LoadAutoGroupedStreamKernel creates a typed launcher
            // that implicitly uses the default accelerator stream.
            // In order to create a launcher that receives a custom accelerator stream
            // use: accelerator.LoadAutoGroupedKernel<Index1, ArrayView<int> int>(...)
            var candleKernel = accelerator.LoadAutoGroupedStreamKernel<
                Index1,
                ArrayView<double>,
                ArrayView<double>,
                ArrayView<double>,
                ArrayView<double>,
                ArrayView<double>,
                ArrayView<double>,
                ArrayView<double>,
                ArrayView<double>
                > (CandleKernel);

            var smaKernel = accelerator.LoadAutoGroupedStreamKernel<
                Index1,
                ArrayView<double>,
                ArrayView<double>, int>
                (SmaKernel);

            // Allocate some memory
            using var openBuffer = accelerator.Allocate<double>(open);
            using var highBuffer = accelerator.Allocate<double>(high);
            using var lowBuffer = accelerator.Allocate<double>(low);
            using var closeBuffer = accelerator.Allocate<double>(close);
            using var bodyBuffer = accelerator.Allocate<double>(close.Length);
            using var sizeBuffer = accelerator.Allocate<double>(close.Length);
            using var upperBuffer = accelerator.Allocate<double>(close.Length);
            using var lowerBuffer = accelerator.Allocate<double>(close.Length);
            using var smaBuffer = accelerator.Allocate<double>(close.Length);

            // Launch buffer.Length many threads and pass a view to buffer
            smaKernel(closeBuffer.Length, closeBuffer.View, smaBuffer.View, smaPeriod);
            candleKernel(closeBuffer.Length, openBuffer, highBuffer, lowBuffer, closeBuffer, bodyBuffer, sizeBuffer, upperBuffer, lowerBuffer);

            // Wait for the kernel to finish...
            accelerator.Synchronize();
            body = bodyBuffer.GetAsArray();
            size = sizeBuffer.GetAsArray();
            upperShadow = upperBuffer.GetAsArray();
            lowerShadow = lowerBuffer.GetAsArray();
            sma = smaBuffer.GetAsArray();

            using var smaBodyLongBuffer = accelerator.Allocate<double>(body.Length);
            using var smaBodyShortBuffer = accelerator.Allocate<double>(body.Length);
            using var smaBodyVeryShortBuffer = accelerator.Allocate<double>(body.Length);

            smaKernel(body.Length, bodyBuffer.View, smaBodyLongBuffer.View, this.candleSettings[(int)CandleSettingType.BodyLong].AvgPeriod);
            smaKernel(body.Length, bodyBuffer.View, smaBodyShortBuffer.View, this.candleSettings[(int)CandleSettingType.BodyShort].AvgPeriod);
            smaKernel(body.Length, bodyBuffer.View, smaBodyVeryShortBuffer.View, this.candleSettings[(int)CandleSettingType.BodyVeryShort].AvgPeriod);

            // Wait for the kernel to finish...
            accelerator.Synchronize();

            date = bars.Date.ToArray();
            if (emaPeriod > 0)
                ema = close.EmaSeries(emaPeriod);

            // Resolve data


            smaBodyLong = smaBodyLongBuffer.GetAsArray();
            smaBodyShort = smaBodyShortBuffer.GetAsArray();
            smaBodyVeryShort = smaBodyVeryShortBuffer.GetAsArray();


        }
#endif


        /// <summary>
        /// Calculates all the required the simple moving averages.
        /// </summary>
        /// <param name="bars">bars contains high,low, close, volume</param>
        /// <param name="smaPeriod">the SMA period to look for a trend</param>
        /// <param name="emaPeriod">the EMA period to look for a trend</param>
        /// 
        void InitialiseAverages(Bars bars, int smaPeriod, int emaPeriod)
        { 
            // Set up the 
            this.smaPeriod = smaPeriod;
            this.emaPeriod = emaPeriod;

            open = bars.Open.ToArray();
            high = bars.High.ToArray();
            low = bars.Low.ToArray();
            close = bars.Close.ToArray();


            date = bars.Date.ToArray();
            if (close.Length != bars.Close.Count)
            {
                System.Diagnostics.Debug.WriteLine("Length != Count");
            }

            date = bars.Date.ToArray();
            count = close.Length;
            name = bars.Name;
            if( smaPeriod > 0 )
                sma = close.SmaSeries(smaPeriod);
            
            if( emaPeriod > 0)
                ema = close.EmaSeries(emaPeriod);

            body = new double[count];
            upperShadow = new double[count];
            lowerShadow = new double[count];
            size = new double[count];
            for (int i=0; i<count; i++)
            {
                body[i] = Math.Abs(open[i] - close[i]);
                upperShadow[i] = high[i] - Math.Max(open[i], close[i]);
                lowerShadow[i] = Math.Min(open[i], close[i]) - low[i];
                size[i] = high[i] - low[i];
            }

            smaBodyLong = body.SmaSeries(this.candleSettings[(int)CandleSettingType.BodyLong].AvgPeriod);
            smaBodyShort = body.SmaSeries(this.candleSettings[(int)CandleSettingType.BodyShort].AvgPeriod);
            smaBodyVeryShort = body.SmaSeries(this.candleSettings[(int)CandleSettingType.BodyVeryShort].AvgPeriod);
        }


        /// <summary>
        /// Puts all the candlestick methods into an array.
        /// </summary>
        void InitialiseMethods()
        {
            candleMethods = new (CandleMethod, string name)[]
            {
                (Find_2BlackGapping, "2BlackGapping"),
                (Find_2Crows, "2Crows"),
                (Find_3BlackCrows, "3BlackCrows"),
                (Find_3InsideDown, "3Inside-"),
                (Find_3InsideUp, "3Inside"),
                (Find_3OutsideDown,"3Outside-"),
                (Find_3OutsideUp,"3Outside-"),
                (Find_3LineStrikeBearish,"3LineStrike-"),
                (Find_3LineStrikeBullish,"3LineStrike"),
                (Find_3MethodsFalling,"3Methods-"),
                (Find_3MethodsRising,"3Methods"),
                (Find_3StarsSouth, "3Stars-"),
                (Find_3WhiteSoldiers, "3WhiteSoldiers"),
                (Find_AbandonedBabyBearish,"AbandonedBaby-"),
                (Find_AbandonedBabyBullish,"AbandonedBaby"),
                (Find_AboveTheStomach,"AboveStomach"),
                (Find_AdvanceBlock,"AdvanceBlock"),
                (Find_BelowTheStomach,"BelowStomach"),
                (Find_BeltHoldBearish,"BeltHold-"),
                (Find_BeltHoldBullish,"BeltHold"),
                (Find_BreakawayBearish, "Breakaway-"),
                (Find_BreakawayBullish, "Breakaway"),
                (Find_ConcealingBabySwallow, "ConcealBabySwallow"),
                (Find_Deliberation,"Deliberation"),
                (Find_DojiStarBearish, "DojiStar-"),
                (Find_DojiStarBullish, "DojiStar"),
                (Find_DownsideGap3Methods,"Gap3Methods-"),
                (Find_EngulfingBearish,"Engulfing-"),
                (Find_EngulfingBullish,"Engulfing"),
                (Find_EveningDojiStar,"EveningDojiStar"),
                (Find_EveningStar,"EveningStar"),
                (Find_Hammer, "Hammer"),
                (Find_HammerInverted, "Hammer-"),
                (Find_HomingPigeon, "HomingPigeon"),
                (Find_Identical3Crows, "Identical3Crows"),
                (Find_InNeck, "InNeck"),
                (Find_Kicking, "Kicking"),
                // (Find_LadderBottom, "Ladder-"), // do I bother
                // (Find_LastEngulfingBottom, "LastEngulfing-"),
                // (Find_LastEngulfingTop, "LastEngulfing"),
                (Find_MatchingLow, "MatchingLow"),
                (Find_MatHold, "MatHold"),
                (Find_MeetingLinesBearish, "MeetingLines-"),
                (Find_MeetingLinesBullish, "MeetingLines"),
                (Find_MorningDojiStar,"MorningDojiStar"),
                (Find_MorningStar,"MorningStar"),
                (Find_OnNeck,"OnNeck"),
                (Find_Piercing, "Piercing"),
                // (Find_SeparatingLinesBearish, "SeparatingLines-"),
                // (Find_SeparatingLinesBullish, "SeparatingLines"),
                // (Find_SideBySideWhite, "SideBySideWhite"),
                (Find_StickSandwich, "StickSandwich"),
                (Find_TasukiGapDownside, "TasukiGap-"),
                (Find_TasukiGapUpside, "TasukiGap"),
                (Find_Thrusting, "Thrusting"),
                (Find_TriStarBearish, "TriStar-"),
                (Find_TriStarBullish, "TriStar"),
                // (Find_UpsideGap3Methods,"Gap3Methods"),
                // (Find_Unique3RiverBottom, "3River-"),
                // (Find_UpsideGap2Crows, "Gap2Crows"),
            };
        }
#endregion Constructors

#region Candle helpers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsBlackMarubozu(int i)
        {
            return IsBodyLong(i) && high[i] == close[i] && low[i] == open[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsWhiteMarubozu(int i)
        {
            return IsBodyLong(i) && high[i] == open[i] && low[i] == close[i];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsBlack(int i)
        {
            return close[i] > open[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsWhite(int i)
        {
            return open[i] > close[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsBodyLong(int i, int offset=1)
        {
            return body[i] >= smaBodyLong[i-offset] * candleSettings[(int)CandleSettingType.BodyLong].Factor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsBodyShort(int i, int offset = 1)
        {
            return body[i] < smaBodyShort[i - offset] * candleSettings[(int)CandleSettingType.BodyShort].Factor;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsSizeLong(int i, int offset = 1)
        {
            return size[i] > size[i - offset] * candleSettings[(int)CandleSettingType.BodyLong].Factor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsNear(double a, double b)
        {
            if (a  >= 1 && b >= 1)
            {
                return Math.Abs(a - b) < 0.05;
            }
            else
            {
                return Math.Abs(a - b) < 0.025;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsDownwardTrend(int i)
        {
            if (ema != null && close[i] < ema[i])
                return true;

            if (sma == null)
                return false;

            if (sma[i] < sma[i - 1])
                count += 1;
            if (sma[i - 1] < sma[i - 2])
                count += 1;
            if (sma[i - 2] < sma[i - 3])
                count += 1;
            if (sma[i - 3] < sma[i - 4])
                count += 1;
            if (sma[i - 4] < sma[i - 5])
                count += 1;

            return count >= 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsUpwardTrend(int i)
        {
            if (ema != null && close[i] > ema[i])
                return true;

            if (sma == null)
                return false;

            if (sma[i] > sma[i - 1])
                count += 1;
            if (sma[i - 1] > sma[i - 2])
                count += 1;
            if (sma[i - 2] > sma[i - 3])
                count += 1;
            if (sma[i - 3] > sma[i - 4])
                count += 1;
            if (sma[i - 4] > sma[i - 5])
                count += 1;

            return count >= 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsDoji(int i)
        {
            // Allow a few pennies between the opening and closing prices so as not to exclude too many patterns
            // Shadow length doesn't matter
            return body[i] <= Math.Log(close[i]) && body[i] <= smaBodyVeryShort[i] * candleSettings[(int) CandleSettingType.BodyVeryShort].Factor ;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        double UpperShadow(int i)
        {
            if (open[i] > close[i])
                return high[i] - open[i];
            else
                return high[i] - close[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        double LowerShadow(int i)
        {
            if (open[i] > close[i])
                return close[i] - low[i];
            else
                return open[i] - low[i];
        }

#region LookBack
        /// <summary>
        /// Generic lookback method, which returns how far back moving averages should start.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        private int LookBack(int no_of_candles)
        {
            // 1 candle line
            return no_of_candles + Math.Max(smaPeriod, emaPeriod);
        }
#endregion LookBack
#endregion Candle helpers

#region Candle calculations
#region 2BlackGapping
        int LookBack_2BlackGapping()
        {
            return 2 + Math.Max(smaPeriod, emaPeriod);
        }

        bool Is2BlackGapping(int i)
        {
            return
                // Gaps down from the prior day and forms a black candle
                high[i - 2] < low[i - 1] && IsBlack(i - 1)

                // A lower high forms on the second black candle
                && high[i] < high[i - 1]
                ? IsDownwardTrend(i - 2) : false;
        }

        public List<(string name, int bar)> Find_2BlackGapping(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length-startIdx : startIdx, LookBack_2BlackGapping()); i < close.Length; i++)
            {
                if (Is2BlackGapping(i))
                {
                    results.Add((name: "2BlackGapping", bar: i));
                }
            }

            return results;
        }
#endregion 2BlackGapping

#region 2Crows
        int LookBack_2Crows()
        {
            return 2 + Math.Max(smaPeriod, emaPeriod);
        }
        bool Is2Crows(int i)
        {
            return
                IsBodyLong(i - 2) && IsWhite(i - 2) && IsBlack(i - 1) && IsBlack(i) && close[i - 1] > open[i - 2] && open[i] <= open[i - 1] && open[i] >= close[i - 1] && close[i] <= close[i - 2] && close[i] >= open[i - 2]
                ? IsUpwardTrend(i - 3) : false;
        }

        public List<(string name, int bar)> Find_2Crows(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_2Crows()); i < close.Length; i++)
            {
                if (Is2Crows(i))
                {
                    results.Add((name: name, bar: i));
                }
            }


            return results;
        }
#endregion 2Crows

#region 3BlackCrows
        int LookBack_3BlackCrows()
        {
            return 2 + Math.Max(smaPeriod, emaPeriod);
        }

        bool Is3BlackCrows(int i)
        {
            return
                IsBlack(i - 2) && IsBlack(i - 1) && IsBlack(i)
                && open[i - 1] >= close[i - 2] && open[i - 1] <= open[i - 2] && open[i] >= close[i - 1] && open[i] <= open[i - 1]
                && IsNear(close[i - 2], low[i - 2]) && IsNear(close[i - 1], low[i - 1]) && IsNear(close[i], low[i])
                ? IsUpwardTrend(i - 3) : false;
        }

        public List<(string name, int bar)> Find_3BlackCrows(int startIdx = 60)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length-startIdx : startIdx, LookBack_3BlackCrows()); i < close.Length; i++)
            {
                if (Is3BlackCrows(i))
                {
                    results.Add((name: "3BlackCrows", bar: i));
                }
            }


            return results;
        }
#endregion 3BlackCrows

#region 3InsideDown
        int LookBack_3InsideDown()
        {
            return 3 + Math.Max(smaPeriod, emaPeriod); // Include IsBodyLong
        }

        bool Is3InsideDown(int i)
        {
            return
                // First day: A tall white candle
                IsBodyLong(i - 2) && IsWhite(i - 2)
                // Second day: A small black candle. 
                && IsBodyShort(i - 1) && IsBlack(i - 1)
                // The open and close must be within the body of the first day,
                && ((open[i - 1] <= close[i - 2] && close[i - 1] > open[i - 2])
                // Either the tops or the bottoms can be equal, but not both,
                || (open[i - 1] < close[i - 2] && close[i - 1] >= open[i - 2]))
                // Third day: Usually a black candle, price must close lower
                && close[i] <= close[i - 2]
                ? IsUpwardTrend(i - 3) : false;
        }

        public List<(string name, int bar)> Find_3InsideDown(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_3InsideDown()); i < close.Length; i++)
            {
                if (Is3InsideDown(i))
                {
                    results.Add((name: "3Inside-", bar: i));
                }
            }

            return results;
        }
#endregion 3InsideDown

#region 3InsideUp
        int LookBack_3InsideUp()
        {
            return 2 + Math.Max(smaPeriod, emaPeriod);
        }

        bool Is3InsideUp(int i)
        {
            return
                // First day: A tall white candle
                IsBodyLong(i - 2) && IsBlack(i - 2)
                // Second day: A small white candle. 
                && IsBodyShort(i - 1) && IsWhite(i - 1)
                // The open and close must be within the body of the first day,
                && ((close[i - 1] <= open[i - 2] && open[i - 1] > close[i - 2])
                // Either the tops or the bottoms can be equal, but not both.
                || (close[i - 1] < open[i - 2] && open[i - 1] >= close[i - 2]))
                // Third day: price must close higher.
                && IsWhite(i) && close[i] > close[i - 1]
                ? IsUpwardTrend(i - 3) : false;
        }

        public List<(string name, int bar)> Find_3InsideUp(int startIdx = 60)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_3InsideUp()); i < close.Length; i++)
            {
                if (Is3InsideUp(i))
                {
                    results.Add((name: "3Inside", bar: i));
                }
            }

            return results;
        }
#endregion 3InsideUp

#region 3LineStrikeBearish
        int LookBack_3LineStrikeBearish()
        {
            return 3 + Math.Max(smaPeriod, emaPeriod);
        }

        bool Is3LineStrikeBearish(int i)
        {
            return 
                IsWhite(i) 
                && IsBlack(i - 1) && IsBlack(i - 2) && IsBlack(i - 3) && open[i] < close[i - 1] && close[i] > open[i - 3] && close[i - 1] < close[i - 2] && close[i - 2] < close[i - 3]
                ? IsDownwardTrend(i - 4) : false;
        }

        public List<(string name, int bar)> Find_3LineStrikeBearish(int startIdx = 60)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_3LineStrikeBearish()); i < close.Length; i++)
            {
                if (Is3LineStrikeBearish(i))
                {
                    results.Add((name: "3LineStrike-", bar: i));
                }
            }

            return results;
        }
#endregion 3LineStrikeBearish

#region 3LineStrikeBullish
        int LookBack_3LineStrikeBullish()
        {
            return 3 + Math.Max(smaPeriod, emaPeriod);
        }

        bool Is3LineStrikeBullish(int i)
        {
            return
                IsBlack(i)
                && IsWhite(i - 1)
                && IsWhite(i - 2)
                && IsWhite(i - 3) && close[i] < open[i - 1] && open[i] > close[i - 3] && open[i - 1] < close[i - 2] && close[i - 2] < close[i - 3]
                ? IsDownwardTrend(i - 4) : false;
        }

        public List<(string name, int bar)> Find_3LineStrikeBullish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length-startIdx : startIdx, LookBack_3LineStrikeBullish()); i < close.Length; i++)
            {
                if (Is3LineStrikeBullish(i))
                {
                    results.Add((name: "3LineStrike-", bar: i));
                }
            }

            return results;
        }
#endregion 3LineStrikeBullish

#region 3MethodsFalling
        const int Candles_3MethodsFalling = 5;

        bool Is3MethodsFalling(int i)
        {
            return
                IsBlack(i-4) && IsSizeLong(i-4)
                && IsWhite(i-3) && high[i - 3] < high[i - 4] && low[i - 3] > low[i - 4] 
                && high[i-2] < high[i-4] && low[i-2] > low[i-4] && close[i-2] > close[i-3]
                && IsWhite(i-1) && high[i - 1] < high[i - 1] && low[i - 1] > low[i - 4] && close[i - 1] > close[i - 2]
                && IsBlack(i) && IsSizeLong(i,5) && close[i] < close[i-4] 
                ? IsDownwardTrend(i - Candles_3MethodsFalling) : false;
        }

        public List<(string name, int bar)> Find_3MethodsFalling(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_3MethodsFalling)); i < close.Length; i++)
            {
                if (Is3MethodsFalling(i))
                {
                    results.Add((name: "3Methods-", bar: i));
                }
            }

            return results;
        }
#endregion 3MethodsFalling

#region 3MethodsRising
        const int Candles_3MethodsRising = 5;

        bool Is3MethodsRising(int i)
        {
            return
                IsWhite(i - 4) && IsSizeLong(i - 4)
                && IsBlack(i - 3) && high[i - 3] < high[i - 4] && low[i - 3] > low[i - 4]
                && high[i - 2] < high[i - 4] && low[i - 2] > low[i - 4] && close[i - 2] < close[i - 3]
                && IsBlack(i - 1) && high[i - 1] < high[i - 1] && low[i - 1] > low[i - 4] && close[i - 1] < close[i - 2]
                && IsWhite(i) && IsSizeLong(i, 5) && close[i] > close[i - 4]
                ? IsUpwardTrend(i - Candles_3MethodsRising) : false;
        }

        public List<(string name, int bar)> Find_3MethodsRising(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_3MethodsRising)); i < close.Length; i++)
            {
                if (Is3MethodsFalling(i))
                {
                    results.Add((name: "3Methods", bar: i));
                }
            }

            return results;
        }
#endregion 3MethodsRising

#region 3OutsideDown
        int LookBack_3OutsideDown()
        {
            return 2 + Math.Max(emaPeriod, smaPeriod);
        }

        bool Is3OutsideDown(int i)
        {
            return
                // First day: A white candle
                IsWhite(i - 2)
                // Second day: A black candle opens higher and closes lower than the prior candles body, engulfing it
                && IsBlack(i - 1) && open[i - 1] > open[i - 2] && close[i - 1] < open[i - 2]
                // Last day: A candle with a lower close
                && close[i] < close[i - 1]
                // An upward trend leading to the start of the candle pattern
                ? IsUpwardTrend(i - 3) : false;
        }

        public List<(string name, int bar)> Find_3OutsideDown(int startIdx = 60)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_3OutsideDown()); i < close.Length; i++)
            {
                if (Is3OutsideDown(i))
                {
                    results.Add((name: "3Outside-", bar: i));
                }
            }

            return results;
        }
#endregion 3OutsideDown

#region 3OutsideUp
        int LookBack_3OutsideUp()
        {
            // Number of candle lines = 3
            return 2 + Math.Max(emaPeriod, smaPeriod);
        }

        bool Is3OutsideUp(int i)
        {
            return
                // First day: A black candle
                IsBlack(i - 2)
                // Second day: A white candle opens below the prior candles body and closes above the body too.
                && IsWhite(i - 1) && open[i - 1] < close[i - 2] && close[i - 1] > open[i - 2]
                // Last day: A white candle in which price closes higher
                && IsWhite(i) && close[i] > close[i - 1]
                // A Downward trend leading to the start of the candle pattern
                ? IsDownwardTrend(i - 3) : false;
        }

        public List<(string name, int bar)> Find_3OutsideUp(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_3OutsideUp()); i < close.Length; i++)
            {
                if (Is3OutsideUp(i))
                    results.Add((name: "3Outside", bar: i));
            }

            return results;
        }
#endregion 3OutsideUp

#region 3StarsSouth
        const int Candles_3StarsSouth = 3;

        bool Is3StarsSouth(int i)
        {
            return
                // First day: A tall black candle with a longer shadow
                IsBlack(i - 2) && IsBodyLong(i - 2) && lowerShadow[i - 2] > body[i - 2]
                // Second day: Similar to the first day but smaller and with a low above the previous day's low
                && IsBlack(i-1) &&  size[i-1] < size[i-2] && low[i-1] > low[i-2]
                // Last day: A black marubozu type candle fits inside the high-low trading range of the prior day
                && IsBlack(i) && open[i] == high[i] && close[i] == low[i] && close[i] >= low[i-1] && open[i] <= high[i-1]
                // Downward trendleading to the start of the body pattern
                ? IsDownwardTrend(i - Candles_3StarsSouth) : false;
        }

        public List<(string name, int bar)> Find_3StarsSouth(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack(Candles_3StarsSouth)); i < close.Length; i++)
            {
                if (Is3StarsSouth(i))
                {
                    results.Add((name: "3StarsSouth", bar: i));
                }
            }

            return results;
        }
#endregion 3StarsSouth

#region 3WhiteSoldiers
        const int Candles_3WhiteSoldiers = 3;

        bool Is3WhiteSoldiers(int i)
        {
            return
                // Three days: Tall white candles with higher closes and p;rice that opens within the previous body. Price should close near the high each day
                IsWhite(i-2) && IsBodyLong(i-2) && IsNear(close[i - 2], high[i - 2])
                && IsWhite(i) && IsBodyLong(i - 1,2) && close[i - 1] > close[i - 2] && open[i-1] > close[i-2] && open[i-1] < close[i-2]  && IsNear(close[i-2],high[i-2])
                && IsWhite(i) && IsBodyLong(i,3) && close[i] > close[i - 1] && open[i] > close[i - 1] && open[i] < close[i - 1] && IsNear(close[i - 2], high[i - 2])
                ? IsDownwardTrend(i - Candles_3WhiteSoldiers) : false;
        }

        public List<(string name, int bar)> Find_3WhiteSoldiers(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack(Candles_3WhiteSoldiers)); i < close.Length; i++)
            {
                if (Is3WhiteSoldiers(i))
                {
                    results.Add((name: "3WhiteSoldiers", bar: i));
                }
            }

            return results;
        }
#endregion 3WhiteSoldiers

#region AbandonedBabyBearish
        const int Candles_AbandonedBabyBearish = 3;
        int LookBack_AbandonedBabyBearish()
        {
            // 1 candle line
            return Candles_AbandonedBabyBearish + Math.Max(smaPeriod, emaPeriod);
        }

        bool IsAbandonedBabyBearish(int i)
        {
            return
                // First day: A white candle 
                IsWhite(i - 2) 
                // Second day: A doji whose lower shadows gaps above the prior and following days' highs
                && IsDoji(i - 1) && low[i-1] > high[i-2] && low[i-1] > high[i]
                // Last day: A black candle with the upper shadow remaining below the doji's lower shadow
                && IsBlack(i) 
                // Downward trendleading to the start of the body pattern
                ? IsUpwardTrend(i - Candles_AbandonedBabyBearish) : false;
        }

        public List<(string name, int bar)> Find_AbandonedBabyBearish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack_AbandonedBabyBearish()); i < close.Length; i++)
            {
                if (IsAbandonedBabyBearish(i))
                {
                    results.Add((name: "AbandonedBaby-", bar: i));
                }
            }

            return results;
        }
#endregion AbandonedBabyBearish

#region AbandonedBabyBullish
        const int Candles_AbandonedBabyBullish = 3;

        bool IsAbandonedBabyBullish(int i)
        {
            return
                // First day: A black candle 
                IsBlack(i - 2)
                // Second day: A doji whose upper shadows gaps below the prior and following days' lows
                && IsDoji(i - 1) && high[i - 1] < low[i - 2] && high[i - 1] < low[i]
                // Last day: A white candle with the upper shadow remaining below the doji's lower shade
                && IsWhite(i) 
                // Downward trendleading to the start of the body pattern
                ? IsDownwardTrend(i - Candles_AbandonedBabyBullish) : false;
        }

        public List<(string name, int bar)> Find_AbandonedBabyBullish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack(Candles_AbandonedBabyBullish)); i < close.Length; i++)
            {
                if (IsAbandonedBabyBullish(i))
                {
                    results.Add((name: "AbandonedBaby", bar: i));
                }
            }

            return results;
        }
#endregion AbandonedBabyBearish

#region AboveTheStomach
        int LookBack_AboveTheStomach()
        {
            // 2 candle lines
            return 2 + Math.Max(smaPeriod, emaPeriod);
        }

        bool IsAboveTheStomach(int i)
        {
            return
                // First day: A black candle
                IsBlack(i-1)
                // Second day: White candle opening and closing at or above the midpoint of the prior black candle's body
                && IsWhite(i) && open[i] >= (open[i - 1] + close[i - 1]) / 2 && close[i] >= (open[i - 1]+close[i - 1])/2 
                // Downward trend
                ? IsDownwardTrend(i - 2) : false;
        }

        public List<(string name, int bar)> Find_AboveTheStomach(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length-startIdx : startIdx, LookBack_AboveTheStomach()); i < close.Length; i++)
            {
                if (IsAboveTheStomach(i))
                {
                    results.Add((name: "AboveStomach", bar: i));
                }
            }

            return results;
        }
#endregion 2BlackGapping

#region AdvanceBlock
        const int Candles_AdvanceBlock = 3;
        bool IsAdvanceBlock(int i)
        {
            return
                // Candle color: White for all three candles
                IsWhite(i-2) && IsWhite(i-2) && IsWhite(i)
                // Open: Price must open within the previous body
                && open[i-1] >= open[i-2] && open[i - 1] <= close[i - 2] && open[i] >= open[i - 1] && open[i] <= close[i - 1]
                // Shadows: Talls on days two and three
                && lowerShadow[i-1]+upperShadow[i-1] > lowerShadow[i - 2] + upperShadow[i - 2] && lowerShadow[i] + upperShadow[i] > lowerShadow[i - 1] + upperShadow[i - 1]
                ? IsUpwardTrend(i - 3) : false;
        }

        public List<(string name, int bar)> Find_AdvanceBlock(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_AdvanceBlock)); i < close.Length; i++)
            {
                if (IsAboveTheStomach(i))
                {
                    results.Add((name: "AdvanceBlock", bar: i));
                }
            }

            return results;
        }
#endregion AdvanceBlock

#region BelowTheStomach
        int LookBack_BelowTheStomach()
        {
            // 2 candle lines
            return 2 + Math.Max(smaPeriod, emaPeriod);
        }

        bool IsBelowTheStomach(int i)
        {
            return
                // First day: A tall white day
                IsBodyLong(i-1) && IsWhite(i - 1)
                // Second day: The candle opens below the middle of the white candle's body and closes at or below the middle too
                && open[i] < (open[i-1]+close[i-1]) / 2 && close[i] <= (open[i-1] + close[i-1]) / 2 
                // Downward trend
                ? IsUpwardTrend(i - 2) : false;
        }

        public List<(string name, int bar)> Find_BelowTheStomach(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_BelowTheStomach()); i < close.Length; i++)
            {
                if (IsBelowTheStomach(i))
                {
                    results.Add((name: "BelowStomach", bar: i));
                }
            }

            return results;
        }
#endregion 2BlackGapping

#region BeltHoldBearish
        int LookBack_BeltHoldBearish()
        {
            // 1 candle line
            return 1 + Math.Max(smaPeriod, emaPeriod);
        }

        bool IsBeltHoldBearish(int i)
        {
            return
                // Downward trend
                IsUpwardTrend(i - 1)
                // Prices opens at the high
                && high[i] == open[i]
                // And closes near the low
                && IsNear(low[i], close[i]);
        }

        public List<(string name, int bar)> Find_BeltHoldBearish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length-startIdx : startIdx, LookBack_BeltHoldBearish()); i < close.Length; i++)
            {
                if (IsBeltHoldBearish(i))
                results.Add((name: "BeltHold-", bar: i));
            }

            return results;
        }
#endregion BeltHoldBearish

#region BeltHoldBullish
        const int Candles_BeltHoldBullish = 1;
        int LookBack_BeltHoldBullish()
        {
            // 1 candle line
            return Candles_BeltHoldBullish + Math.Max(smaPeriod, emaPeriod) - 1;
        }

        bool IsBeltHoldBullish(int i)
        {
            return
                // A tall white candle with price opening at the low
                IsWhite(i) && IsBodyLong(i) && low[i] == open[i]
                // And closing near the high
                && IsNear(low[i], close[i])
                // Downward trend
                ? IsDownwardTrend(i - Candles_BeltHoldBullish) : false;
        }

        public List<(string name, int bar)> Find_BeltHoldBullish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack_BeltHoldBullish()); i < close.Length; i++)
            {
                if (IsBeltHoldBullish(i))
                {
                    results.Add((name: "BeltHold", bar: i));
                }
            }

            return results;
        }
#endregion BeltHoldBullish

#region BreakawayBearish
        const int Candles_BreakawayBearish = 5;
        int LookBack_BreakawayBearish()
        {
            // 1 candle line
            return Candles_BreakawayBearish + Math.Max(smaPeriod, emaPeriod);
        }

        bool IsBreakawayBearish(int i)
        {
            return
                // First day: A tall white candle 
                IsWhite(i-4) && IsBodyLong(i-4) 
                // Second day: A white candle that has a gap between the two candle bodyes
                && IsWhite(i-3) && open[i-3] > close[i-4]
                // Third day: A candle with a higher close
                && close[i-2] > close[i-3]
                // Fourth day: A white candle with a higher close
                && IsWhite(i-1) && close[i-1] > close[i-2]
                // Last day: A tall black candle within the gap between the first two body candles
                && IsBlack(i) && IsBodyLong(i) && close[i] > close[i-4] && close[i] < open[i-3]
                // Upward trendleading to the start of the body pattern
                ? IsUpwardTrend(i - Candles_BreakawayBearish) : false;
        }

        public List<(string name, int bar)> Find_BreakawayBearish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack_BreakawayBearish()); i < close.Length; i++)
            {
                if (IsBreakawayBearish(i))
                {
                    results.Add((name: "Breakaway-", bar: i));
                }
            }

            return results;
        }
#endregion BreakawayBearish

#region BreakawayBullish
        const int Candles_BreakawayBullish = 5;
        int LookBack_BreakawayBullish()
        {
            // 1 candle line
            return Candles_BreakawayBullish + Math.Max(smaPeriod, emaPeriod);
        }

        bool IsBreakawayBullish(int i)
        {
            return
                // First day: A tall black candle 
                IsBlack(i - 4) && IsBodyLong(i - 4)
                // Second day: A black candle that has a gap between the two candle bodies
                && IsBlack(i - 3) && open[i - 3] < close[i - 4]
                // Third day: A candle with a lower close
                && close[i - 2] < close[i - 3]
                // Fourth day: A black candle with a lower close
                && IsBlack(i - 1) && close[i - 1] < close[i - 2]
                // Last day: A tall white candle within the gap between the first two body candles
                && IsWhite(i) && IsBodyLong(i) && close[i] < close[i - 4] && close[i] > open[i - 3]
                // Downward trendleading to the start of the body pattern
                ? IsDownwardTrend(i - Candles_BreakawayBullish) : false;
        }

        public List<(string name, int bar)> Find_BreakawayBullish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack_BreakawayBullish()); i < close.Length; i++)
            {
                if (IsBreakawayBullish(i))
                {
                    results.Add((name: "Breakaway", bar: i));
                }
            }

            return results;
        }
#endregion BreakawayBullish

#region ConcealingBabySwallow
        const int Candles_ConcealingBabySwallow = 4;
        int LookBack_ConcealingBabySwallow()
        {
            // 1 candle line
            return Candles_ConcealingBabySwallow + Math.Max(smaPeriod, emaPeriod);
        }

        bool IsConcealingBabySwallow(int i)
        {
            return
                // First and second day: Two long black candles without any shadows (both are black marubozu candles).
                IsBlackMarubozu(i - 3) && IsBlackMarubozu(i - 2)
                // Third day: A black candle with a tall upper shadow. The candle gaps open downward and yet trades into the body of the prior day.
                && IsBlack(i-1) && upperShadow[i] > body[i] + lowerShadow[i] && open[i-1] < close[i-2] && high[i-1] < open[i-2] && high[i-1] > close[i-2]
                // Last day: Another black day that completely engulfs the prior day, including the shadws
                && IsBlack(i - 1) && low[i] <= low[i-1] && high[i] >= high[i-1]
                // Downward trendleading to the start of the body pattern
                ? IsDownwardTrend(i - Candles_ConcealingBabySwallow) : false;
        }

        public List<(string name, int bar)> Find_ConcealingBabySwallow(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack_ConcealingBabySwallow()); i < close.Length; i++)
            {
                if (IsConcealingBabySwallow(i))
                {
                    results.Add((name: "ConcealBabySwallow", bar: i));
                }
            }

            return results;
        }
#endregion ConcealingBabySwallow

#region DarkCloudCover
        int LookBack_DarkCloudCover()
        {
            return 2 + Math.Max(smaPeriod, emaPeriod);
        }

        bool IsDarkCloudCover(int i)
        {
            return IsWhite(i - 1) && IsBodyLong(i - 1) && IsBlack(i) && open[i] > high[i - 1] && close[i] < (close[i - 1] + open[i - 1]) / 2;
        }

        public List<(string name, int bar)> Find_DarkCloudCover(int startIdx = 0, int w = 3)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_DarkCloudCover()); i < close.Length; i++)
            {
                if (IsDarkCloudCover(i))
                {
                    if (IsUpwardTrend(i - 1))
                        results.Add((name: "DarkCloudCover", bar: i));
                }
            }

            return results;
        }
#endregion DarkCloudCover

#region Deliberation
        int LookBack_Deliberation()
        {
            return 2 + Math.Max(smaPeriod, emaPeriod);
        }

        bool IsDeliberation(int i)
        {
            return
                IsWhite(i - 2) && IsWhite(i - 1) && IsWhite(i - 2)
                && open[i] > open[i - 1] && open[i - 1] > open[i - 2]
                && close[i] > close[i - 1] && close[i - 1] > close[i - 2]
                && IsBodyLong(i - 1) && IsBodyLong(i - 2) && IsBodyShort(i)
                && IsNear(open[i], close[i - 1])
                ? IsUpwardTrend(i - 2) : false;
        }

        public List<(string name, int bar)> Find_Deliberation(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length-startIdx : startIdx, LookBack_Deliberation()); i < close.Length; i++)
            {
                if (IsDeliberation(i))
                {
                    results.Add((name: "Deliberation", bar: i));
                }
            }

            return results;
        }
#endregion Deliberation

#region DojiStarBearish
        int LookBack_DojiStarBearish()
        {
            return 2 + Math.Max(smaPeriod, emaPeriod);
        }

        bool IsDojiStarBearish(int i)
        {
            return 
                IsWhite(i-1) && IsBodyLong(i-1) && IsDoji(i) && (UpperShadow(i) + LowerShadow(i)) < body[i]
                ? IsUpwardTrend(i - 2) : false;
        }

        public List<(string name, int bar)> Find_DojiStarBearish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_DojiStarBearish()); i < close.Length; i++)
            {
                if (IsDojiStarBearish(i))
                {
                    results.Add((name: "DojiStar-", bar: i));
                }
            }

            return results;
        }
#endregion DojiStarBearish

#region DojiStarBullish
        int LookBack_DojiStarBullish()
        {
            return 1 + Math.Max(smaPeriod, emaPeriod);
        }
        bool IsDojiStarBullish(int i)
        {
            var pattern = IsBlack(i - 1) && IsBodyLong(i - 1) && IsDoji(i) && (UpperShadow(i) + LowerShadow(i)) < body[i - 1];
            if (!pattern)
                return false;
            return IsDownwardTrend(i - 2);
        }

        public List<(string name, int bar)> Find_DojiStarBullish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length-startIdx : startIdx, LookBack_DojiStarBullish()); i < close.Length; i++)
            {
                if (IsDojiStarBullish(i))
                {
                    results.Add((name: "DojiStar", bar: i));
                }
            }

            return results;
        }
#endregion DojiStarBullish

#region DownsideGap3Methods
        const int Candles_DownsideGap3Methods = 3;
        int LookBack_DownsideGap3Methods()
        {
            return Candles_DownsideGap3Methods + Math.Max(smaPeriod, emaPeriod);
        }
        bool IsDownsideGap3Methods(int i)
        {
            return
                // First day: a long black-bodied candle
                IsBlack(i - 2) && IsBodyLong(i - 2)
                // Second day: Another long black bodied candle with a gap between today and yesterday, including the shadows
                && IsBlack(i - 1) && IsBodyLong(i - 1) && high[i-1] < low[i-2]
                // Third day: Price forms a white candle. The candle opens with the body of the second day and closes within the body of the first candle
                && IsWhite(i) && open[i] >= close[i-1] && open[i] <= open[i-1] && close[i] >= close[i-2] && close[i] <= open[i-2]
                ? IsDownwardTrend(i - Candles_DownsideGap3Methods) : false;
        }

        public List<(string name, int bar)> Find_DownsideGap3Methods(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_DojiStarBullish()); i < close.Length; i++)
            {
                if (IsDownsideGap3Methods(i))
                {
                    results.Add((name: "Gap3Methods-", bar: i));
                }
            }

            return results;
        }
#endregion DownsideGap3Methods

#region EngulfingBearish
        int LookBack_EngulfingBearish()
        {
            return 1 + Math.Max(smaPeriod, emaPeriod);
        }
        bool IsEngulfingBearish(int i)
        {
            return 
                IsWhite(i - 1) && IsBlack(i) && close[i] >= open[i - 1] && open[i] <= close[i - 1] 
                ? IsUpwardTrend(i - 2) : false;
        }

        /// <summary>
        /// This two-candle pattern begins with a while candle followed by a black one. 
        /// The black candle has a body that is taller than the white candle's body that it overlaps.
        /// </summary>
        /// <param name="i">bar</param>
        /// <returns>bool</returns>
        public List<(string name, int bar)> Find_EngulfingBearish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_EngulfingBearish()); i < close.Length; i++)
            {
                if (IsEngulfingBearish(i))
                {
                    results.Add((name: "Engulfing-", bar: i));
                }
            }

            return results;
        }
#endregion EngulfingBearish

#region EngulfingBullish
        int LookBack_EngulfingBullish()
        {
            return 1 + Math.Max(smaPeriod, emaPeriod);
        }
        bool IsEngulfingBullish(int i)
        {
            return 
                IsBlack(i - 1) && IsWhite(i) && open[i] >= close[i - 1] && close[i] <= open[i - 1]
                ? IsDownwardTrend(i - 2) : false;
        }
        public List<(string name, int bar)> Find_EngulfingBullish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();


            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack_EngulfingBullish()); i < close.Length; i++)
            {
                if (IsEngulfingBullish(i))
                    results.Add((name: "Engulfing", bar: i));
            }

            return results;
        }
#endregion EngulfingBullish

#region EveningDojiStar
        int Candles_EveningDojiStar = 3;

        bool IsEveningDojiStar(int i)
        {
            var body2Top = Math.Max(open[i - 1], close[i - 1]);

            return
                // First day: A tall white candle 
                IsWhite(i - 2) && IsBodyLong(i - 2)
                // Second day: A small bodied candle that gaps above the bodies of the adjacent candles
                && IsDoji(i - 1) && body2Top > close[i - 2] && body2Top > open[i]
                // Last day: A tall black candle that closes at least halfway down the body of the white candle
                && IsBlack(i) && IsSizeLong(i) && close[i] <= (open[i - 2] + close[i - 2]) / 2
                // Downward trendleading to the start of the body pattern
                ? IsUpwardTrend(i - Candles_EveningDojiStar) : false;
        }

        public List<(string name, int bar)> Find_EveningDojiStar(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack(Candles_EveningDojiStar)); i < close.Length; i++)
            {
                if (IsEveningDojiStar(i))
                {
                    results.Add((name: "EveningDojiStar", bar: i));
                }
            }

            return results;
        }
#endregion EveningDojiStar

#region EveningStar
        int Candles_EveningStar = 3;

        bool IsEveningStar(int i)
        {
            var body2Top = Math.Max(open[i - 1], close[i - 1]);

            return
                // First day: A tall white candle 
                IsWhite(i - 2) && IsBodyLong(i-2)
                // Second day: A small bodied candle that gaps above the bodies of the adjacent candles
                && IsBodyShort(i - 1) && body2Top > close[i - 2] && body2Top > open[i]
                // Last day: A tall black candle that closes at least halfway down the body of the white candle
                && IsBlack(i) && close[i] <= (open[i-2]+close[i-2])/2
                // Downward trendleading to the start of the body pattern
                ? IsUpwardTrend(i - Candles_EveningStar) : false;
        }

        public List<(string name, int bar)> Find_EveningStar(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack(Candles_EveningStar)); i < close.Length; i++)
            {
                if (IsEveningStar(i))
                {
                    results.Add((name: "EveningStar", bar: i));
                }
            }

            return results;
        }
#endregion EveningStar

#region Hammer
        int Candles_Hammer = 1;

        bool IsHammer(int i)
        {
            var lowerShadow = LowerShadow(i);
            var shortBody = smaBodyShort[i - 1] * candleSettings[(int)CandleSettingType.BodyShort].Factor ;
            return
                // Has a lower shadow between two and three times the height of a small body 
                lowerShadow >= shortBody * 2 && lowerShadow <= shortBody * 3
                // and little or no upper shadow
                && open[i] == high[i] // no
                // Downward trendleading to the start of the body pattern
                ? IsDownwardTrend(i - Candles_Hammer) : false;
        }

        public List<(string name, int bar)> Find_Hammer(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack(Candles_Hammer+1)); i < close.Length; i++)
            {
                if (IsHammer(i))
                {
                    results.Add((name: "Hammer", bar: i));
                }
            }

            return results;
        }
#endregion Hammer

#region HammerInverted
        int Candles_HammerInverted = 2;

        bool IsHammerInverted(int i)
        {
            var upperShadow = UpperShadow(i);
            var shortBody = smaBodyShort[i - 1] * candleSettings[(int)CandleSettingType.BodyShort].Factor ;
            return
                // First day: A tall black candle with a close near the low of the day
                IsSizeLong(i - 1) && IsNear(close[i], low[i])
                // Second day: A small body candle 
                && IsBodyShort(i) && upperShadow >= smaBodyShort[i - 1] * 2 && lowerShadow[i] < float.Epsilon
                // Downward trendleading to the start of the body pattern
                ? IsDownwardTrend(i - Candles_HammerInverted) : false;
        }

        public List<(string name, int bar)> Find_HammerInverted(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack(Candles_HammerInverted + 1)); i < close.Length; i++)
            {
                if (IsHammer(i))
                {
                    results.Add((name: "Hammer-", bar: i));
                }
            }

            return results;
        }
#endregion Hammer

#region HomingPigeon
        const int Candles_HomingPigeon = 2;

        bool IsHomingPigeon(int i)
        {
            return
                // First day: A tall black body
                IsBodyLong(i - 1) && IsBlack(i-1)
                // Second day: A short black body that is inside the body of the first day
                && IsBodyShort(i) && IsBlack(i) && close[i] >= close[i-1] && open[i] <= open[i-1]
                // Downward trendleading to the start of the body pattern
                ? IsDownwardTrend(i - Candles_HomingPigeon) : false;
        }

        public List<(string name, int bar)> Find_HomingPigeon(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack(Candles_HomingPigeon)); i < close.Length; i++)
            {
                if (IsHomingPigeon(i))
                {
                    results.Add((name: "HomingPigeon", bar: i));
                }
            }

            return results;
        }
#endregion HomingPigeon

#region Identical3Crows
        const int Candles_Identical3Crows = 3;

        bool IsIdentical3Crows(int i)
        {
            return
                // Configuration: Look for three tall black candles,
                IsBlack(i-2) && IsBodyLong(i-2) && IsBlack(i - 1) && IsBodyLong(i - 1,2) && IsBlack(i) && IsBodyLong(i - 1, 3)
                // Configuration: the last two each opoen at or near the prior close
                && IsNear(open[i-1], close[i - 2]) && IsNear(open[i],close[i-1])
                // Downward trendleading to the start of the body pattern
                ? IsUpwardTrend(i - Candles_Identical3Crows) : false;
        }

        public List<(string name, int bar)> Find_Identical3Crows(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx, LookBack(Candles_Identical3Crows)); i < close.Length; i++)
            {
                if (IsIdentical3Crows(i))
                {
                    results.Add((name: "Identical3Crows", bar: i));
                }
            }

            return results;
        }
#endregion Identical3Crows

#region InNeck
        int Candles_InNeck = 2;

        bool IsInNeck(int i)
        {
            return
                // First day: A tall black candle 
                IsBlack(i - 1) && IsSizeLong(i - 1)
                // Second day: A white candle with an open below the low of the first day and a close that is in the body of the first day, but not by much
                && IsWhite(i) && open[i] < low[i-1] && close[i] > close[i-1] && IsNear(close[i], close[i-1]) && close[i] < open[i-1]
                ? IsDownwardTrend(i - Candles_InNeck) : false;
        }

        public List<(string name, int bar)> Find_InNeck(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_InNeck + 1)); i < close.Length; i++)
            {
                if (IsInNeck(i))
                {
                    results.Add((name: "InNeck", bar: i));
                }
            }

            return results;
        }
#endregion InNeck

#region Kicking
        int Candles_Kicking = 2;

        bool IsKicking(int i)
        {
            return
                // First day: A marobozu candle: a tall black candle with no shadows
                IsBlack(i - 1) && IsBodyLong(i - 1) && upperShadow[i - 1] <= float.Epsilon && lowerShadow[i - 1] <= float.Epsilon
                // Second day: Price gaps higher and a white mauboz candle forms: a tall white candle with no shadows
                && close[i] < close[i - 1] && IsWhite(i - 1) && IsBodyLong(i) && upperShadow[i] <= float.Epsilon && lowerShadow[i - 1] <= float.Epsilon;
        }

        public List<(string name, int bar)> Find_Kicking(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_Kicking + 1)); i < close.Length; i++)
            {
                if (IsKicking(i))
                {
                    results.Add((name: "Kicking", bar: i));
                }
            }

            return results;
        }
#endregion InNeck

#region MorningDojiStar
        int Candles_MorningDojiStar = 3;

        bool IsMorningDojiStar(int i)
        {
            return
                // First day: A tall black candle 
                IsBlack(i - 2) && IsSizeLong(i - 2)
                // Second day: A doji whose body gaps below the prior body
                && IsDoji(i-1) && Math.Min(open[i-1],close[i-1]) < close[i-2]
                // Third day: A tall white candle whose body remains above the doji's body
                && IsWhite(i) && IsSizeLong(i) && open[i] > Math.Max(open[i - 1], close[i - 1]) 
                // Downward trendleading to the start of the body pattern
                ? IsDownwardTrend(i - Candles_MorningDojiStar) : false;
        }

        public List<(string name, int bar)> Find_MorningDojiStar(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_MorningDojiStar)); i < close.Length; i++)
            {
                if (IsMorningDojiStar(i))
                {
                    results.Add((name: "MorningDojiStar", bar: i));
                }
            }

            return results;
        }
#endregion MorningStar

#region MorningStar
        int Candles_MorningStar = 3;

        bool IsMorningStar(int i)
        {
            return
                // First day: A tall black candle 
                IsBlack(i - 2) && IsBodyLong(i - 2)
                // Second day: A small bodied candle that gaps lower from the prior body
                && IsBodyShort(i - 1) && Math.Max(open[i-1],close[i-1]) < close[i-2]
                // Last day: A tall white candle that gaps above the body of the second day and closes at least midway into the body of the 1st day
                && IsWhite(i) && open[i] > Math.Max(open[i - 1], close[i - 1]) && close[i] >= (open[i-2] + close[i-2]) / 2
                // Downward trendleading to the start of the body pattern
                ? IsDownwardTrend(i - Candles_MorningStar) : false;
        }

        public List<(string name, int bar)> Find_MorningStar(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_MorningStar)); i < close.Length; i++)
            {
                if (IsMorningStar(i))
                {
                    results.Add((name: "MorningStar", bar: i));
                }
            }

            return results;
        }
#endregion MorningStar

#region MatchingLow
        int Candles_MatchingLow = 2;

        bool IsMatchingLow(int i)
        {
            return
                // First day: A tall black candle 
                IsBlack(i - 1) && IsBodyLong(i - 1)
                // Second day: A black candle with a close that matches the prior close
                && IsBlack(i) && close[i] == close[i-1]
                ? IsDownwardTrend(i - Candles_MatchingLow) : false;
        }

        public List<(string name, int bar)> Find_MatchingLow(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_MatchingLow)); i < close.Length; i++)
            {
                if (IsMatchingLow(i))
                {
                    results.Add((name: "MatchingLow", bar: i));
                }
            }

            return results;
        }
#endregion MatchingLow

#region MatHold
        int Candles_MatHold = 5;

        bool IsMatHold(int i)
        {
            return
                // First day: A tall black candle 
                IsWhite(i - 4) && IsSizeLong(i - 4)
                // Second day: A price gap opens upward but closes lower
                && IsBlack(i-3) && close[i-3] > close[i-4] && open[i-3] > close[i-4] &&  IsBodyShort(i-3)
                // Third day: Any color, small body closing price easing lower, but body remain above first day
                && IsBodyShort(i-2) && close[i-2] < close[i-3] && Math.Min(open[i],close[i]) > low[i-4]
                // Fourth day: A black candle, small body closing price easing lower, but body remain above first day
                && IsBlack(i-1) && IsBodyShort(i - 1) && close[i - 2] < close[i - 3] && close[i] > low[i - 4]
                // Fifth day: A white candle with a close above the highs of the prior four candles
                && IsWhite(i) && close[i] > high[i-1] && close[i] > high[i-2] && close[i] > high[i-3] && close[i] > high[i-4]
                ? IsUpwardTrend(i - Candles_MatHold) : false;
        }

        public List<(string name, int bar)> Find_MatHold(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_MatHold)); i < close.Length; i++)
            {
                if (IsMatHold(i))
                {
                    results.Add((name: "MatHold", bar: i));
                }
            }

            return results;
        }
#endregion MatchingLow

#region MeetingLinesBearish
        int Candles_MeetingLinesBearish = 2;

        bool IsMeetingLinesBearish(int i)
        {
            return
                // First day: A tall white candle 
                IsWhite(i - 1) && IsSizeLong(i - 1)
                // Second day: A tall black candle that closes at or near the prior day's close
                && IsBlack(i) && IsSizeLong(i) && IsNear(close[i],close[i-1])
                ? IsUpwardTrend(i - Candles_MeetingLinesBearish) : false;
        }

        public List<(string name, int bar)> Find_MeetingLinesBearish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_MeetingLinesBearish)); i < close.Length; i++)
            {
                if (IsMeetingLinesBearish(i))
                {
                    results.Add((name: "MeetingLines-", bar: i));
                }
            }

            return results;
        }
#endregion MatchingLow

#region MeetingLinesBullish
        int Candles_MeetingLinesBullish = 2;

        bool IsMeetingLinesBullish(int i)
        {
            return
                // First day: A tall black candle 
                IsBlack(i - 1) && IsSizeLong(i - 1)
                // Second day: A tall white candle that closes at or near the prior day's close
                && IsWhite(i) && IsSizeLong(i) && IsNear(close[i], close[i - 1])
                ? IsDownwardTrend(i - Candles_MeetingLinesBearish) : false;
        }

        public List<(string name, int bar)> Find_MeetingLinesBullish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_MeetingLinesBullish+1)); i < close.Length; i++)
            {
                if (IsMeetingLinesBullish(i))
                {
                    results.Add((name: "MeetingLines", bar: i));
                }
            }

            return results;
        }
#endregion MeetingLinesBullish

#region OnNeck
        int Candles_OnNeck = 2;

        bool IsOnNeck(int i)
        {
            return
                // First day: A tall black candle 
                IsBlack(i - 1) && IsSizeLong(i - 1)
                // Second day: A tall white candle that closes at or near the prior day's close
                && IsWhite(i) && close[i] == low[i-1]
                ? IsDownwardTrend(i - Candles_MeetingLinesBearish) : false;
        }

        public List<(string name, int bar)> Find_OnNeck(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_OnNeck + 1)); i < close.Length; i++)
            {
                if (IsOnNeck(i))
                {
                    results.Add((name: "OnNeck", bar: i));
                }
            }

            return results;
        }
#endregion OnNeck

#region Piercing
        int Candles_Piercing = 2;

        bool IsPiercing(int i)
        {
            return
                // First day: A tall black candle 
                IsBlack(i - 1) 
                // Second day: A white candle that opens below the prior candle's dlow and closes in the black body, between the midpoint and the open
                && IsWhite(i) && open[i] < low[i - 1] && close[i] <= open[i-1] && close[i] >= (open[i-1] + close[i-1])/2
                ? IsDownwardTrend(i - Candles_MatchingLow) : false;
        }

        public List<(string name, int bar)> Find_Piercing(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_Piercing)); i < close.Length; i++)
            {
                if (IsPiercing(i))
                {
                    results.Add((name: "Piercing", bar: i));
                }
            }

            return results;
        }
#endregion Piercing

#region StickSandwich
        int Candles_StickSandwich = 3;

        bool IsStickSandwich(int i)
        {
            return
                // First day: A tall black candle 
                IsBlack(i - 2)
                // Second day: A white candle that trades above the close of the first day
                && IsWhite(i-1) && open[i-1] > high[i-2]
                // Third day: A black candle that closes at or near the close of the first day
                && IsNear(close[i],close[i-2])
                ? IsDownwardTrend(i - Candles_MatchingLow) : false;
        }

        public List<(string name, int bar)> Find_StickSandwich(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_StickSandwich)); i < close.Length; i++)
            {
                if (IsPiercing(i))
                {
                    results.Add((name: "StickSandwich", bar: i));
                }
            }

            return results;
        }
#endregion StickSandwich

#region TasukiGapDownside
        int Candles_TasukiGapDownside = 3;

        bool IsTasukiGapDownside(int i)
        {
            return
                // First day: A white candlestick
                IsBlack(i - 2) 
                // Second day: A black candle. Price gaps down including the shadows.
                && IsBlack(i - 1) && low[i - 2] > high[i - 1]
                // Third day: A black candle opens in the body of the prior candle and closes within the gap between the shadows.
                && IsWhite(i) && open[i] >= close[i-1] && open[i] <= open[i-1] && close[i] > high[i-1] && close[i] < low[i-2]
                ? IsDownwardTrend(i - Candles_MatchingLow) : false;
        }

        public List<(string name, int bar)> Find_TasukiGapDownside(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_TasukiGapDownside)); i < close.Length; i++)
            {
                if (IsTasukiGapDownside(i))
                {
                    results.Add((name: "TasukiGap-", bar: i));
                }
            }

            return results;
        }
#endregion IsTasukiGapDownside

#region TasukiGapUpside
        int Candles_TasukiGapUpside = 3;

        bool IsTasukiGapUpside(int i)
        {
            return
                // First day: A white candlestick
                IsWhite(i - 2)
                // Second day: A white candle. Price gaps high including the shadows.
                && IsWhite(i - 1) && low[i - 1] > high[i - 2]
                // Third day: A black candle opens in the body of the prior candle and closes within the gap between the shadows.
                && IsBlack(i) && open[i] >= open[i - 1] && open[i] <= close[i - 1] && close[i] < low[i - 1] && close[i] > high[i - 2]
                ? IsUpwardTrend(i - Candles_MatchingLow) : false;
        }

        public List<(string name, int bar)> Find_TasukiGapUpside(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_TasukiGapUpside)); i < close.Length; i++)
            {
                if (IsPiercing(i))
                {
                    results.Add((name: "TasukiGap", bar: i));
                }
            }

            return results;
        }
#endregion TasukiGapUpside

#region Thrusting
        int Candles_Thrusting = 2;

        bool IsThrusting(int i)
        {
            var midPointPriorBody = (open[i - 1] + close[i - 1]) / 2;
            return
                // First day: A black candle 
                IsBlack(i - 1)
                // Second day: A white candle that opens below the prior low and closes near but below the midpoint of the prior body
                && IsWhite(i) && open[i] < low[i - 1] && close[i] < midPointPriorBody && IsNear(close[i], midPointPriorBody)
                ? IsDownwardTrend(i - Candles_Thrusting) : false;
        }

        public List<(string name, int bar)> Find_Thrusting(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_Thrusting)); i < close.Length; i++)
            {
                if (IsThrusting(i))
                {
                    results.Add((name: "Thrusting", bar: i));
                }
            }

            return results;
        }
#endregion Thrusting

#region TriStarBearish
        const int Candles_TriStarBearish = 3;

        bool IsTriStarBearish(int i)
        {
            return
                // Configuration: Look for 3 doji, the middle one has a body above the other two
                IsDoji(i-2) && IsDoji(i - 1) && IsDoji(i)
                // the middle one has a body above the other two
                && Math.Max(open[i - 1],close[i - 1]) > Math.Max(open[i - 2], close[i - 2])
                && Math.Max(open[i - 1], close[i - 1]) > Math.Max(open[i], close[i])
                ? IsUpwardTrend(i - Candles_TriStarBearish) : false;
        }

        public List<(string name, int bar)> Find_TriStarBearish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_TriStarBearish)); i < close.Length; i++)
            {
                if (IsTriStarBearish(i))
                {
                    results.Add((name: "TriStar-", bar: i));
                }
            }

            return results;
        }
#endregion TriStarBearish

#region TriStarBullish
        const int Candles_TriStarBullish = 3;

        bool IsTriStarBullish(int i)
        {
            return
                // Configuration: Look for 3 doji, the middle one has a body above the other two
                IsDoji(i - 2) && IsDoji(i - 1) && IsDoji(i)
                // the middle one has a body above the other two
                && Math.Min(open[i - 1], close[i - 1]) < Math.Min(open[i - 2], close[i - 2])
                && Math.Min(open[i - 1], close[i - 1]) < Math.Min(open[i], close[i])
                ? IsDownwardTrend(i - Candles_TriStarBullish) : false;
        }

        public List<(string name, int bar)> Find_TriStarBullish(int startIdx = 0)
        {
            var results = new List<(string name, int bar)>();

            for (int i = Math.Max(startIdx < 0 ? close.Length - startIdx : startIdx, LookBack(Candles_TriStarBullish)); i < close.Length; i++)
            {
                if (IsTriStarBullish(i))
                {
                    results.Add((name: "TriStar", bar: i));
                }
            }

            return results;
        }
#endregion TriStarBullish
#endregion Candle calculations

#region Find candles
        public string[] GetNames()
        {
            string[] names = new string[candleMethods.Length];
            for (int i = 0; i < candleMethods.Length; i++)
            {
                names[i] = candleMethods[i].name;
            }
            return names;
        }

        public List<(string name, int bar)> FindProfitable(Data.DataSet dataSet, MarketType marketType, int n, RatioType ratioType, double ratio = 1.5, int startIdx = 0 )
        {
            // Read in the candle data
            List<Data.CandleData>candleDatas = Data.CandleData.GetDataSet(dataSet.Guid);
            
            // Eliminate divisions by zero
            candleDatas = candleDatas.FindAll(cd => cd.NegativeSum > 0 && cd.PositiveSum > 0 && (cd.MarketType == marketType || cd.MarketType == MarketType.All));
            
            // Only distinct ones
            candleDatas = candleDatas.GroupBy(x=>x.Name).Select(y => y.First()).ToList();
            
            // Select by actual profitRatio > than specificed profitRatio
            candleDatas.Sort((cd1, cd2) => (cd2.PositiveSum / cd2.NegativeSum).CompareTo(cd1.PositiveSum / cd1.NegativeSum));

            string[] names;
            switch(ratioType)
            {
                case RatioType.PositiveSum:
                    names = candleDatas.Where(cd => cd.N >= n && cd.PositiveSum / cd.NegativeSum >= ratio).Select(cd => cd.Name).ToArray();
                    break;
                case RatioType.PositiveN:
                    names = candleDatas.Where(cd => cd.N >= n && cd.PositiveN / cd.NegativeN >= ratio).Select(cd => cd.Name).ToArray();
                    break;
                case RatioType.ExpectedReturn:
                    names = candleDatas.Where(cd => cd.N >= n && cd.ExpectedReturn > ratio).Select(cd => cd.Name).ToArray();
                    break;
                default:
                    return new List<(string name, int bar)>(); // for the compiler
            }


            return FindNames(names,startIdx) ;
        }

        public List<(string name, int bar)> FindName(string name, int startIdx=0)
        {
            var results = new List<(string name, int bar)>();

            int index = Array.FindIndex(candleMethods,m => m.name.ToUpper() == name.ToUpper());

            if (index > -1)
            {
                results = candleMethods[index].method(startIdx);
            }
            return results;
        }

        public List<(string name, int bar)> FindNames(string[] names, int startIdx = 0)
        {
            object lockFindNames = new object();
            var results = new List<(string name, int bar)>();
            var methods = new List<(CandleMethod method, string name)>();
            foreach( var name in names)
            {
                var index = Array.FindIndex<(CandleMethod method,string name)>(this.candleMethods, m => m.name == name);
                if( index != -1 )
                {
                    methods.Add(this.candleMethods[index]);
                }
            }

            Parallel.ForEach(methods, method =>
            {
                var result = method.method(startIdx);

                lock (lockFindNames)
                {
                    results.AddRange(result);
                }
            });

            return results;
        }

        
        public List<(string name, int bar)> FindAll(int startIdx=0)
        {
            object lockFindAll = new object();

            var results = new List<(string name, int bar)>();

            // Parallel.ForEach(candleMethods,candleMethod=>
            foreach( var candleMethod in candleMethods)
            {
                try
                {
                    var result = candleMethod.method(startIdx);
                    // if (outInteger[i] != 0)  System.Diagnostics.Debug.Print(method.name + " " + i.ToString()+", "+ outInteger[i].ToString());

                    lock (lockFindAll)
                    {
                        results.AddRange(result);
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Error reading candle");
                }
            } //);
            return results;
        }
#endregion Find candles
    }
}
