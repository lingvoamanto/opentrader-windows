using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
#if __IOS__
using UIKit;
using Foundation;
#endif
#if __MACOS__
using AppKit;
using Foundation;
#endif
#if __WINDOWS__
using System.ComponentModel;
using System.Runtime.CompilerServices;
#endif

namespace OpenTrader.Data
{
    [DataContract]
    public partial class DataSet
#if __WINDOWS__
        : INotifyPropertyChanged
#endif
    {
        public DataSets datasets;

        public string Path;
        public string mName;
        public string mYahooPrefix;
        public string mYahooSuffix;
        public string mExchange;
        public string mYahooIndex;
#if __WINDOWS__
        public event PropertyChangedEventHandler PropertyChanged;
#endif
        [DataMember]
        public double Liquidity { get; set; }

        public DataFiles DataFiles { get; set; }

        [DataMember]
        public string Name
        {
            get { return mName; }
            set
            {
                mName = value;
                // TODO: datasets.Replace(this);
            }
        }

        public override string ToString()
        {
            return mName;
        }

        public Preferences Preferences
        {
            get { return datasets.Preferences; }
        }

#if __IOS__
        public UIViewController ParentViewController
        {
            get { return datasets.ParentViewController; }
        }
#endif
#if __MACOS__
        public NSViewController ParentViewController
        {
            get { return datasets.ParentViewController; }
        }
#endif

        [DataMember]
        public string YahooPrefix
        {
            get { return mYahooPrefix; }
            set
            {
                mYahooPrefix = value;
                // TODO: datasets.Replace(this);
            }
        }

        [DataMember]
        public string YahooSuffix
        {
            get { return mYahooSuffix; }
            set
            {
                mYahooSuffix = value;
                // TODO: datasets.Replace(this);
            }
        }

        [DataMember]
        public string Exchange
        {
            get { return mExchange; }
            set
            {
                mExchange = value;
                // TODO: datasets.Replace(this);
            }
        }

        [DataMember]
        public string YahooIndex
        {
            get { return mYahooIndex; }
            set
            {
                mYahooIndex = value;
                // TODO: datasets.Replace(this);
            }
        }

        public DataSet(DataSets datasets, string path)
        {
            this.datasets = datasets;
            this.Path = path;
        }

#if __WINDOWS__
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
#endif

    }
}

