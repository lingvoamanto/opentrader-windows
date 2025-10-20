using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;

namespace OpenTrader.Indicators
{
    public class CandleSetting
    {
        CandleRangeType candleRangeType;
        CandleSettingType candleSettingType;
        int avgPeriod; 
        double factor;

        static public CandleSetting[] Defaults
        {
            get
            {
                var settings = Enum.GetValues(typeof(CandleSettingType)).Cast<int>();

                var defaults = new CandleSetting[settings.Max()+1];
                defaults[(int)CandleSettingType.BodyLong] = new CandleSetting(10, 1.0);
                defaults[(int)CandleSettingType.BodyVeryLong] = new CandleSetting(10, 3.0);
                defaults[(int)CandleSettingType.BodyShort] = new CandleSetting(10, 1.0);
                defaults[(int)CandleSettingType.BodyVeryShort] = new CandleSetting(10, 0.1);
                defaults[(int)CandleSettingType.ShadowLong] = new CandleSetting(10, 1.0);
                defaults[(int)CandleSettingType.ShadowVeryLong] = new CandleSetting(10, 2.0);
                defaults[(int)CandleSettingType.ShadowShort] = new CandleSetting(10, 1.0);
                defaults[(int)CandleSettingType.ShadowVeryShort] = new CandleSetting(10, 0.1);
                defaults[(int)CandleSettingType.SizeLong] = new CandleSetting(10, 1.0);
                defaults[(int)CandleSettingType.SizeVeryLong] = new CandleSetting(10, 3.0);
                defaults[(int)CandleSettingType.SizeShort] = new CandleSetting(10, 1.0);
                defaults[(int)CandleSettingType.SizeVeryShort] = new CandleSetting(10, 1.0);
                return defaults;
            }
        }

        public CandleSetting(CandleSettingType candleSettingType, int avgPeriod, double factor)
        {
            this.candleSettingType = candleSettingType;
            this.avgPeriod = avgPeriod;
            this.factor = factor;
        }

        private CandleSetting(int avgPeriod, double factor)
        {
            this.avgPeriod = avgPeriod;
            this.factor = factor;
        }

        public CandleSettingType CandleSettingType { get => candleSettingType; set => candleSettingType = value; }

        public CandleRangeType CandleRangeType { get => candleRangeType; set => candleRangeType = value; }
        public int AvgPeriod { get=> avgPeriod; set=> avgPeriod = value; }
        public double Factor { get=>factor; set=>factor=value; }
    }
}