using System;
namespace OpenTrader
{
    public enum PositionType { LongPosition, ShortPosition };
    public enum PositionStatus { Active, Closed };

    public class Position
    {
        int mOpenBar;  // the bar at which the position is opened
        int mCloseBar; // the bar at which the position is close
        double mOpenPrice;  // the price of the trade at open
        double mClosePrice; // the price of the trade at close
        public string OpenSignal;
        public string CloseSignal;

        PositionType mType;
        PositionStatus mStatus;

        double mProfit = 0;

        public PositionType PositionType
        {
            get { return mType; }
        }


        public Position(int bar, double price, PositionType type)
        {
            mOpenBar = bar;
            mOpenPrice = price;
            mType = type;
            mStatus = PositionStatus.Active;
        }

        public void Close(int bar, double price)
        {
            if (mStatus != PositionStatus.Closed)
            {
                mClosePrice = price;
                mCloseBar = bar;
                if (mType == PositionType.LongPosition)
                {
                    mProfit = mClosePrice - mOpenPrice;
                }
                else
                {
                    mProfit = mOpenPrice - mClosePrice;
                }
                mStatus = PositionStatus.Closed;
            }
        }

        public int CloseBar
        {
            get { return mCloseBar; }
        }

        public int OpenBar
        {
            get { return mOpenBar; }
        }

        public PositionStatus Status
        {
            get { return mStatus; }
        }

        public double Profit
        {
            get { return mProfit; }
            set { mProfit = value; }  // allow this to be overridden
        }

        public double ProfitRatio
        {
            get { return (mClosePrice- mOpenPrice) / mOpenPrice; }
        }

        public double OpenPrice
        {
            get { return mOpenPrice; }
        }

        public double ClosePrice
        {
            get { return mClosePrice; }
        }
    }
}

