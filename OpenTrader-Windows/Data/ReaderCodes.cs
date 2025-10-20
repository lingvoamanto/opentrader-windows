using System;
using System.Collections.Generic;

namespace OpenTrader.Data
{
    public class ReaderCodes : List<ReaderCode>
    {
        public ReaderCodes()
        {
        }


        public event OpenTrader.Data.ChangedEvent ReaderCodeChangedEvent;
        public DataFile DataFile;

        void ReaderCodeChanged(ReaderCode item, ChangedEventArgs e)
        {
            if (ReaderCodeChangedEvent != null)
            {
                try
                {
                    ReaderCodeChangedEvent(item, e);
                }
                catch (Exception exception)
                {
                    string message = exception.Message;
                }
            }
        }

        new public void Remove(ReaderCode item)
        {
            ReaderCodeChanged(item, new ChangedEventArgs(ChangedAction.Remove));
            base.Remove(item);
        }

        new public void Add(ReaderCode item)
        {
            item.ReaderCodes = this;
            ReaderCodeChanged(item, new ChangedEventArgs(ChangedAction.Add));
            base.Add(item);
        }

        public void Add(string reader, string code)
        {
            if (this.Find(rc => rc.Reader == reader) == null)
            {
                this.Add(new ReaderCode(this) { Reader = reader, Code = code });
            }
        }

        public void Replace(ReaderCode readercode)
        {
            ReaderCodeChanged(readercode, new ChangedEventArgs(ChangedAction.Replace));
        }

        public string this[string reader]
        {
            get
            {
                ReaderCode readercode = this.Find(rc => rc.Reader == reader);
                if (readercode == null)
                    return "";
                else
                    return readercode.Code;
            }
        }

        public static void ReaderCodeChanged_EventHandler(object oReaderCode, ChangedEventArgs e)
        {
            ReaderCode readerCode = oReaderCode as ReaderCode;

            switch (e.Action)
            {
                case ChangedAction.Add:
                    readerCode.Add();
                    break;
                case ChangedAction.Remove:
                    readerCode.Remove();
                    break;
                case ChangedAction.Replace:
                    readerCode.Replace();
                    break;
            }
        }
    }
}
