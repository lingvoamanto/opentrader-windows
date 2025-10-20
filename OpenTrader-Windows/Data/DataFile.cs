using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using OpenTrader;
#if __IOS__
using UIKit;
using Foundation;
#endif
#if __MACOS__
using AppKit;
using Mono.Data.Sqlite;
using Foundation;
#endif
#if __WINDOWS__
using System.ComponentModel;
using System.Runtime.CompilerServices;
#endif
using System.Collections;


namespace OpenTrader.Data
{
    public delegate void ChangedEventHandler(object sender, EventArgs e);

    public partial class DataFile
# if __WINDOWS__
        : INotifyPropertyChanged
#endif
    {
        private ReaderCodes mReaderCodes = new ReaderCodes();

        public DataFiles datafiles;
        public Bars bars;
        public Bars weekBars;
        public List<bool> interim;
        public List<int> barId;

        public bool Alert { get; set; }
#if __MACOS__
        public NSMutableAttributedString attributedString; // trying to avoid this being null
#endif
#if __IOS__
        public NSMutableAttributedString attributedString; // trying to avoid this being null
#endif
#if __WINDOWS__
        public event PropertyChangedEventHandler PropertyChanged;
#endif
        private Dictionary<string, WeakReference> mCache;
        private Dictionary<string, WeakReference> weekCache;

        public string Title
        {
            get
            {
                if (this.Description == null || this.Description == "")
                {
                    return Name ?? "";
                }
                return Description;
            }
        }

        public ReaderCodes ReaderCodes
        {
            get { return mReaderCodes; }
            set { mReaderCodes = value; }
        }

        public DataSet DataSet
        {
            get => datafiles?.dataset;
        }

#if __IOS__
        public UIKit.UIViewController ParentViewController
        {
            get { return DataSet.ParentViewController; }
        }
#endif
#if __MACOS__
        public AppKit.NSViewController ParentViewController
        {
            get { return DataSet.ParentViewController; }
        }
#endif

        public Preferences Preferences
        {
            get { return DataSet.Preferences; }
        }

        public DataFile(DataFiles datafiles, string path, string name)
        {
            Path = path;
            Name = name;
            Watching = false;
            this.datafiles = datafiles;
            if (datafiles != null)
            {
                if (datafiles.dataset != null)
                {
                    this.DatasetGuid = datafiles.dataset.Guid;
                }
            }

            Initialise();
        }

        public DataFile(int id, string path, string name, string description, string yahooCode, DateTime yahooStart)
        {
            Path = path;
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.YahooCode = yahooCode;
            this.YahooStart = yahooStart;
            Watching = false;

            Initialise();
        }

        public DataFile(int id, string path, string name, string description, string yahooCode, DateTime yahooStart, bool watching)
        {
            Path = path;
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.YahooCode = yahooCode;
            this.YahooStart = yahooStart;
            this.Watching = watching;

            mCache = new Dictionary<string, WeakReference>();
            weekCache = new Dictionary<string, WeakReference>();
            bars = new Bars(name, mCache);
            weekBars = new Bars(name, weekCache);
            interim = new List<bool>();
            barId = new List<int>();
        }


        public event ChangedEventHandler Changed;

        public virtual void OnChanged(EventArgs e)
        {
            mCache.Clear();
            if (Changed != null)
                Changed(this, e);
        }


        public void Read()
        {

            try
            {
                ReadBarsFromFile();
                ConvertBarsToWeeks();
            }
            catch
            {

            }

            OnChanged(EventArgs.Empty);
        }

        public void Sort()
        {
            int n = bars.Count;
            for (int i = 0; i < n; i++)
            {
                for (int j = n - 1; j > i; j--)
                {
                    if (bars.Date[j - 1] > bars.Date[j])
                    {
                        DateTime swapdate = bars.Date[j - 1];
                        bars.Date[j - 1] = bars.Date[j];
                        bars.Date[j] = swapdate;
                        double swapdouble = bars.Open[j - 1];
                        bars.Open[j - 1] = bars.Open[j];
                        bars.Open[j] = swapdouble;
                        swapdouble = bars.High[j - 1];
                        bars.High[j - 1] = bars.High[j];
                        bars.High[j] = swapdouble;
                        swapdouble = bars.Low[j - 1];
                        bars.Low[j - 1] = bars.Low[j];
                        bars.Low[j] = swapdouble;
                        swapdouble = bars.Close[j - 1];
                        bars.Close[j - 1] = bars.Close[j];
                        bars.Close[j] = swapdouble;
                        swapdouble = bars.Volume[j - 1];
                        bars.Volume[j - 1] = bars.Volume[j];
                        bars.Volume[j] = swapdouble;
                        bool swapbool = interim[j - 1];
                        interim[j - 1] = interim[j];
                        interim[j] = swapbool;
                        int swapId = barId[j - 1];
                        barId[j - 1] = barId[j];
                        barId[j] = swapId;
                    }
                }
            }

            // Fire that it's changed even if we're sorting the file
            OnChanged(EventArgs.Empty);
        }

        public void Dedup()
        {
            for (int bar = 0; bar < bars.Count; bar++)
            {
                int index;
                if ((index = bars.Date.FindIndex(bar + 1, d => d.Date == bars.Date[bar])) != -1)
                {
                    bars.Open.RemoveAt(index);
                    bars.Close.RemoveAt(index);
                    bars.Low.RemoveAt(index);
                    bars.High.RemoveAt(index);
                    bars.Volume.RemoveAt(index);
                    bars.Date.RemoveAt(index);
                    interim.RemoveAt(index);
                    barId.RemoveAt(index);
                }
            }
        }

        public void DataFileHeadersChanged_EventHandler(object oDataFile, ChangedEventArgs e)
        {


            switch (e.Action)
            {
                case ChangedAction.Replace:
                    Replace();
                    break;
                default:
                    break;
            }
        }


        public void UpdateFile()
        {
            try
            {
                if (bars.Count == 0)
                {
                    ReadBarsFromFile();
                    this.mCache.Clear();
                    ConvertBarsToWeeks();
                }

                Write();
            }
            catch (Exception exception)
            {
                throw exception;
            }


            OnChanged(EventArgs.Empty);
        }


        public void UpdateWeeks(DateTime startDate)
        {
            DateTime currentWeek = startDate;

            weekBars.Clear();
            this.ConvertBarsToWeeks();
            this.weekCache.Clear();
        }

#if __WINDOWS__
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
#endif
    }
}



