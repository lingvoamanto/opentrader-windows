using System;
using System.Collections.Generic;

namespace OpenTrader
{
    public class Bars 
    {
        public DataSeries Open;
        public DataSeries High;
        public DataSeries Low;
        public DataSeries Close;
        public DataSeries Volume;
        public Dictionary<string, WeakReference> Cache;
        public string Name { get; private set; }
        public List<DateTime> Date;

        internal Bars(string name, Dictionary<string, WeakReference> cache)
        {
            this.Cache = cache;
            Open = new DataSeries("Open", this);
            High = new DataSeries("High", this);
            Low = new DataSeries("Low", this);
            Close = new DataSeries("Close", this);
            Volume = new DataSeries("Volume", this);
            Date = new List<DateTime>();
            Name = name;
        }

        public void ClearCache()
        {
            long memory = GC.GetTotalMemory(true);
            var keys = Cache.Keys;
            List<string> ids = new List<string>();

            foreach(var key in keys)
            {
                ids.Add(key);
            }

            foreach(string id in ids)
            {
                if(! Cache[id].IsAlive)
                {
                    Cache.Remove(id);
                }
            }
            var lCache = Cache;
            GC.Collect();
            memory -= GC.GetTotalMemory(true);
        }


        public int Count
        {
            get
            {
                int max = 0;
                if (Open.Count > max) max = Open.Count;
                if (High.Count > max) max = High.Count;
                if (Low.Count > max) max = Low.Count;
                if (Close.Count > max) max = Close.Count;
                if (Volume.Count > max) max = Volume.Count;
                return max;
            }
        }

        public void Clear()
        {
            Open.Clear();
            High.Clear();
            Low.Clear();
            Close.Clear();
            Volume.Clear();
            Date.Clear();
        }

        public int Find(DateTime date, out bool found)
        {
            int ihigh = Date.Count - 1;
            int ilow = 0;
            int offset = 0;
            int iMid = 0;

            found = false;
            while (ihigh >= ilow && !found)
            {
                iMid = ilow + (ihigh - ilow) / 2;
                if (Date[iMid].Date > date.Date)
                {
                    ihigh = iMid - 1;
                    offset = 0;
                }
                else if (Date[iMid].Date < date.Date)
                {
                    ilow = iMid + 1;
                    offset = 1;
                }
                else
                    found = true;
            }
            return found ? iMid : iMid + offset;
        }

        public void Replace(int index, DateTime date, double open, double high, double low, double close, double volume)
        {
            Open[index] = open;
            High[index] = high;
            Low[index] = low;
            Close[index] = close;
            Volume[index] = volume;
        }

        public void Add(DateTime date, double open, double high, double low, double close, double volume)
        {
            Date.Add(date);
            Open.Add(open);
            High.Add(high);
            Low.Add(low);
            Close.Add(close);
            Volume.Add(volume);
        }

        public void RemoveAt(int index)
        {
            Date.RemoveAt(index);
            Open.RemoveAt(index);
            High.RemoveAt(index);
            Low.RemoveAt(index);
            Close.RemoveAt(index);
            Volume.RemoveAt(index);
        }

        public void Insert(int index, DateTime date, double open, double high, double low, double close, double volume)
        {
            Date.Insert(index, date);
            Open.Insert(index, open);
            High.Insert(index, high);
            Low.Insert(index, low);
            Close.Insert(index, close);
            Volume.Insert(index, volume);
        }
    }
}

