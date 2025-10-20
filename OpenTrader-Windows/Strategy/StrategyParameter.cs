using System;
using System.IO;
using System.Collections.Generic;
#if __MACOS__
using Foundation;
using AppKit;
#endif

namespace OpenTrader
{
    public delegate void ChangedEventHandler(object sender, EventArgs e);

    public class StrategyParameter
    {
        public string Name;
        private double mValue;
        public double Start;
        public double Stop;
        public double Step;
        public StrategyParameters mStrategyParameters;

        public event ChangedEventHandler OnValueChanged;

        public double Value
        {
            get { return mValue; }
            set
            {
                mValue = value;
                if (OnValueChanged != null )
                {
                    OnValueChanged(this, EventArgs.Empty);
                    mStrategyParameters.ValueChanged(this, EventArgs.Empty);
                }
                mStrategyParameters.Replace(this);
            }
        }

        public StrategyParameter(StrategyParameters sp)
        {
            mStrategyParameters = sp;
        }
    }

    public class StrategyParameters : List<StrategyParameter>
    {
        public event ChangedEventHandler OnValueChanged;

        public void ValueChanged(StrategyParameter item, EventArgs e)
        {
            if (OnValueChanged != null)
            {
                OnValueChanged(item, e);
            }
        }

        public void Replace(StrategyParameter item)
        {
            ValueChanged(item, EventArgs.Empty);
        }
    }
}

