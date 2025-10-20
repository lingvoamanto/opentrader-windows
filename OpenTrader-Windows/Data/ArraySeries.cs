using System;
using System.Collections.Generic;

namespace OpenTrader
{
	public partial class ArraySeries
    {
        double[]? data;
		public int FirstValidValue = 0;
		public Bars bars { get; private set; }
		protected Dictionary<string, WeakReference> mCache;

        private string mDescription;

        public Dictionary<string, WeakReference> Cache
        {
            get { return mCache; }
        }

        public string Description
        {
            get { return mDescription; }
        }
		
		public Bars FindParentBars()
        {
            return bars;
        }

        public ArraySeries(string name, Bars bars) 
        {
            this.bars = bars;
            mDescription = name;
            mCache = bars.Cache;
        }

        public ArraySeries(DataSeries ds, string name)
        {
            this.bars = ds.bars;
            mCache = ds.Cache;
            mDescription = name;
            data = ds.ToArray();
        }
		
		public ArraySeries(double[] data, string name, Bars bars)
        {
            this.bars = bars;
            mCache = bars.Cache;
            mDescription = name;
            this.data = new double[data.Length];
			Array.Copy(data,this.data,data.Length);
        }
		
		virtual public int Count
        {
            get { return data.Length; }
        }
		
		virtual public double[] ToArray()
        {
			var result = new double[data.Length];
			Array.Copy(data,result,data.Length);
            return result;
        }

		
		public double this[int index]
        {
            
            get {
                int _index = Math.Min(index, Math.Max(this.Count - 1,0));
                if (_index == -1 || data.Length == 0)
                    return 0;

                return data[_index];
            }
            set { data[index] = value; }
        }
	}
}