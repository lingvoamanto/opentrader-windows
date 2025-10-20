using System;
using System.Collections.Generic;

namespace OpenTrader.Data
{
    public class DataFiles : List<DataFile>
    {
        public event OpenTrader.Data.ChangedEvent? DataFileChangedEvent;
        public DataSet? dataset;

        void DataFileChanged(DataFile item, ChangedEventArgs e)
        {
            if (DataFileChangedEvent != null)
            {
                try
                {
                    DataFileChangedEvent(item, e);
                }
                catch (Exception exception)
                {
                    string message = exception.Message;
                }
            }
        }

        new public void Remove(DataFile item)
        {
            DataFileChanged(item, new ChangedEventArgs(ChangedAction.Remove));
            base.Remove(item);
        }

        new public void Add(DataFile item)
        {
            item.datafiles = this;
            DataFileChanged(item, new ChangedEventArgs(ChangedAction.Add));
            base.Add(item);
        }

        public void Replace(DataFile item)
        {
            DataFileChanged(item, new ChangedEventArgs(ChangedAction.Replace));
            // base.Add( item );			
        }

        public void DataFileDataChanged_EventHandler(object oDataFile, ChangedEventArgs e)
        {
            OpenTrader.Data.DataFile datafile = oDataFile as OpenTrader.Data.DataFile;

            switch (e.Action)
            {
                case ChangedAction.Add:
                    datafile.Add();
                    break;

                case ChangedAction.Remove:
                    datafile.Remove();
                    break;

                case ChangedAction.Replace:
                    {
                        // update the main datafiles table
                        datafile.Replace();
                    }
                    break;
            }
        }


    }

}
