using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenTrader.Data
{
    public class ExternalReaderObject
    {
        public string Name { get; set; }
        private object externalreader;
        public bool CanReadCurrent;
        public bool CanReadHistorical;
        public bool CanReadMany;

        public ExternalReaderObject(object externalreader, string name, bool canreadcurrent, bool canreadhistorical, bool canreadmany)
        {
            this.externalreader = externalreader;
            this.Name = name;
            this.CanReadHistorical = canreadhistorical;
            this.CanReadCurrent = canreadcurrent;
            this.CanReadMany = canreadmany;
        }

        public void ReadHistorical(DataFile datafile)
        {
            try
            {
                Type type = externalreader.GetType();
                type.GetMethod("ReadHistorical").Invoke(externalreader, new object[] { (object)datafile });
            }
            catch
            {

            }
        }

        public void ReadCurrent(DataFile datafile)
        {
            try
            {
                Type type = externalreader.GetType();
                type.GetMethod("ReadCurrent").Invoke(externalreader, new object[] { (object)datafile });
            }
            catch
            {

            }
        }

    }

    public class ExternalReaderList : List<ExternalReaderObject>
    {
        public ExternalReaderList()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                string typename = type.Name;
                if (type.BaseType != null)
                {
                    if (type.BaseType.Name == "ExternalReader" && typename != "ExternalReader")
                    {
                        object externalreader = Assembly.GetExecutingAssembly().CreateInstance(type.FullName);
                        System.Reflection.PropertyInfo nameproperty = type.GetProperty("Name");
                        string name = (string)nameproperty.GetValue(externalreader, null);
                        System.Reflection.PropertyInfo canreadcurrentproperty = type.GetProperty("CanReadCurrent");
                        bool canreadcurrent = (bool)canreadcurrentproperty.GetValue(externalreader, null);
                        System.Reflection.PropertyInfo canreadhistoricalproperty = type.GetProperty("CanReadHistorical");
                        bool canreadhistorical = (bool)canreadhistoricalproperty.GetValue(externalreader, null);
                        System.Reflection.PropertyInfo canreadmanyproperty = type.GetProperty("CanReadMany");
                        bool canreadmany = (bool)canreadmanyproperty.GetValue(externalreader, null);
                        this.Add(new ExternalReaderObject(externalreader, name, canreadcurrent, canreadhistorical, canreadmany));
                    }
                }
            }

            this.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.CurrentCulture));
        }
    }

    public class ExternalReader
    {
        virtual public string Name
        {
            get { return "External"; }
        }

        virtual public bool CanReadCurrent
        {
            get { return false; }
        }

        virtual public bool CanReadHistorical
        {
            get { return false; }
        }

        public ExternalReader()
        {
        }

        virtual public bool CanReadMany
        {
            get { return true; }
        }

        virtual public void ReadCurrent(DataFile datafile)
        {
        }

        virtual public void ReadHistorical(DataFile datafile)
        {
        }

        protected static void AddBar(DataFile datafile, DateTime date, double open, double high, double low, double close, double volume, bool replace)
        {
            Bars bars = datafile.bars;
            List<bool> interim = datafile.interim;
            List<int> barId = datafile.barId;

            // Every price we don't have in this list gets added
            if (bars.Count == 0)
            {
                bars.Add(date, open, high, low, close, volume);
                interim.Add(false);
                // int id = Bar.Save(datafile.YahooCode, date, open, high, low, close, volume, false);
                // barId.Add(id);
            }
            else if (bars.Date[bars.Count - 1] < date)
            {
                bars.Add(date, open, high, low, close, volume);
                interim.Add(false);
                // int id = Bar.Save(datafile.YahooCode, date, open, high, low, close, volume, false);
                // barId.Add(id);
            }
            else if (bars.Date[0] > date)
            {
                bars.Insert(0, date, open, high, low, close, volume);
                interim.Insert(0, false);
                // int id = Bar.Save(datafile.YahooCode, date, open, high, low, close, volume, false);
                // barId.Insert(0, id);
            }
            else
            {
                bool found;
                int index = bars.Find(date, out found);
                if (found)
                {
                    if (interim[index] || replace)
                    {
                        if (Math.Abs(volume) <= double.Epsilon) volume = bars.Volume[index];
                        if (Math.Abs(low) <= double.Epsilon) low = bars.Low[index];
                        if (Math.Abs(open) <= double.Epsilon) open = bars.Open[index];
                        if (Math.Abs(close) <= double.Epsilon) close = bars.Close[index];
                        if (Math.Abs(high) <= double.Epsilon) high = bars.High[index];

                        bars.Replace(index, date, open, high, low, close, volume);
                        interim[index] = false;
                        // int id = Bar.Save(datafile.YahooCode, date, open, high, low, close, volume, false);
                        // barId[index] = 0; 
                    }
                    else
                    {
                        if (Math.Abs(bars.Volume[index]) <= double.Epsilon) bars.Volume[index] = volume;
                        if (Math.Abs(bars.Low[index]) <= double.Epsilon) bars.Low[index] = low;
                        if (Math.Abs(bars.Open[index]) <= double.Epsilon) bars.Open[index] = open;
                        if (Math.Abs(bars.Close[index]) <= double.Epsilon) bars.Close[index] = close;
                        if (Math.Abs(bars.High[index]) <= double.Epsilon) bars.High[index] = high;
                        // int id = Bar.Save(datafile.YahooCode, date, bars.Open[index], bars.High[index], bars.Low[index], bars.Close[index], bars.Volume[index], false);
                        // barId[index] = id;
                    }
                }
                else
                {
                    bars.Insert(index, date, open, high, low, close, volume);
                    interim.Insert(index, false);
                    // int id = Bar.Save(datafile.YahooCode, date, open, high, low, close, volume, false);
                    // barId.Insert(index, id);
                }
            }
        }


    }
}

