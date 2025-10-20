using System;
using System.Collections.Generic;
using OpenTrader.Data;

namespace OpenTrader
{
    public partial class DataSeries : ArraySeries
    {
        List<double> data;



        // this is a special construction for datafiles
        public DataSeries(string name, Bars bars) : base(name,bars)
        {
            data = new List<double>();
        }

        /*		
		public DataSeries( string name, Dictionary<string,DataSeries> Cache  )
		{
			mDescription = name;
			data = new List<double>();
			mCache = Cache;
		}
		*/

        public DataSeries(Bars bars, string name) : base(name, bars)
        {
            data = new List<double>();
            int count = bars.Count;
            for (int bar = 0; bar < bars.Count; bar++)
            {
                Add(0);
            }
        }

        public DataSeries(Bars bars, double[] data, string name) : base(name,bars)
        {
            this.data = new List<double>();
            int count = bars.Count;
            for (int bar = 0; bar < data.Length; bar++)
            {
                Add(0);
            }
        }

        public DataSeries(DataSeries ds, string name) : base(name,ds.bars)
        {
            data = new List<double>();
            foreach (double d in ds.data)
            {
                Add(d);
            }
        }

        public void RemoveAt(int index)
        {
            data.RemoveAt(index);
        }

        public void Add(double value)
        {
            data.Add(value);
        }

        public void Insert(int index, double value)
        {
            data.Insert(index, value);
        }

        public int Count
        {
            get { return data.Count; }
        }

        override public double[] ToArray()
        {
            return data.ToArray();
        }

        public double this[int index]
        {
            
            get {
                int _index = Math.Min(index, Math.Max(this.Count - 1,0));
                if (_index == -1 || data.Count == 0)
                    return 0;

                return data[_index];
            }
            set { data[index] = value; }
        }

        public void Clear()
        {
            data.Clear();
        }

        public static DataSeries operator +(DataSeries ds, double addend)
        {
            string description = ds.Description + "+" + addend.ToString();

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as DataSeries;

            //Create Fisher, cache it, return it
            DataSeries result = new DataSeries(ds, description);
            for (int i = 0; i < ds.Count; i++)
            {
                result[i] = ds[i] + addend;
            }
            return result;
        }

        public static DataSeries operator +(DataSeries ds, DataSeries addend)
        {
            string description = ds.Description + "+" + addend.Description;

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as DataSeries;

            //Create Fisher, cache it, return it
            DataSeries result = new DataSeries(ds, description);
            for (int i = 0; i < ds.Count; i++)
            {
                result[i] = ds[i] + addend[i];
            }
            return result;
        }

        public static DataSeries operator -(DataSeries ds, double subtend)
        {
            string description = ds.Description + "-" + subtend.ToString();

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as DataSeries;

            //Create Fisher, cache it, return it
            DataSeries result = new DataSeries(ds, description);
            for (int i = 0; i < ds.Count; i++)
            {
                result[i] = ds[i] - subtend;
            }
            return result;
        }

        public static DataSeries operator -(DataSeries ds, DataSeries subtend)
        {
            string description = ds.Description + "-" + subtend.Description;

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as DataSeries;

            //Create Fisher, cache it, return it
            DataSeries result = new DataSeries(ds, description);
            for (int i = 0; i < ds.Count; i++)
            {
                result[i] = ds[i] - subtend[i];
            }
            return result;
        }

        public static DataSeries operator *(DataSeries ds, double multiplicand)
        {
            string description = ds.Description + "*" + multiplicand.ToString();

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as DataSeries;

            //Create Fisher, cache it, return it
            DataSeries result = new DataSeries(ds, description);
            for (int i = 0; i < ds.Count; i++)
            {
                result[i] = ds[i] * multiplicand;
            }
            return result;
        }

        public static DataSeries operator *(double multiplicand, DataSeries ds)
        {
            string description = multiplicand.ToString() + "*" + ds.Description;

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as DataSeries;

            //Create Fisher, cache it, return it
            DataSeries result = new DataSeries(ds, description);
            for (int i = 0; i < ds.Count; i++)
            {
                result[i] = ds[i] * multiplicand;
            }
            return result;
        }

        public static DataSeries operator /(DataSeries ds, double divisor)
        {
            string description = ds.Description + "/" + divisor.ToString();

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as DataSeries;

            //Create Fisher, cache it, return it
            DataSeries result = new DataSeries(ds, description);
            for (int i = 0; i < ds.Count; i++)
            {
                result[i] = ds[i] / divisor;
            }
            return result;
        }

        public static DataSeries operator *(DataSeries ds, DataSeries multiplicand)
        {
            string description = ds.Description + "*" + multiplicand.Description;

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as DataSeries;

            //Create Fisher, cache it, return it
            DataSeries result = new DataSeries(ds, description);
            for (int i = 0; i < ds.Count; i++)
            {
                result[i] = ds[i] * multiplicand[i];
            }
            return result;
        }

        public static DataSeries operator /(DataSeries ds, DataSeries divisor)
        {
            string description = ds.Description + "/" + divisor.Description;

            //See if it exists in the cache
            if (ds.Cache.ContainsKey(description))
                return ((WeakReference)ds.Cache[description]).Target as DataSeries;

            //Create Fisher, cache it, return it
            DataSeries result = new DataSeries(ds, description);
            for (int i = 0; i < ds.Count; i++)
            {
                if( divisor[i] != 0)
                { 
                    result[i] = ds[i] / divisor[i];
                }
                else
                {
                    result[i] = double.MaxValue;
                }
            }
            return result;
        }
    }


}

