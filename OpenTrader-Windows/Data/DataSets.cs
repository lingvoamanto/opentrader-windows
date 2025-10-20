using System;
using System.Collections.Generic;
using System.Windows.Documents;
#if __IOS__
using UIKit;
#endif
#if __MACOS__
using AppKit;
#endif

namespace OpenTrader.Data
{
    public class DataSets : List<DataSet>
    {
        public ChangedEvent DataSetChanged;
        private Preferences mPreferences;
#if __IOS__
        private UIViewController mParentViewController;
#endif
#if __MACOS__
        private NSViewController mParentViewController;
#endif
#if __IOS__
        public UIViewController ParentViewController
        {
            get { return mParentViewController; }
            set { mParentViewController = value; }
        }
# endif
#if __MACOS__
        public NSViewController ParentViewController
        {
            get { return mParentViewController; }
            set { mParentViewController = value; }
        }
#endif

        public Preferences Preferences
        {
            set { mPreferences = value; }
            get { return mPreferences; }
        }


        new public void Remove(DataSet item)
        {
            DataSet copy = item;
            base.Remove(item);
            if (DataSetChanged != null)
                DataSetChanged(copy, new ChangedEventArgs(ChangedAction.Remove));
        }

        new public void Add(DataSet item)
        {
            base.Add(item);
            if( DataSetChanged != null)
                DataSetChanged(item, new ChangedEventArgs(ChangedAction.Add));
        }

        public void Replace(DataSet item)
        {
            if (DataSetChanged != null)
                DataSetChanged(item, new ChangedEventArgs(ChangedAction.Replace));
            // base.Add( item );		// why is this here?	
        }

        public void DataSetChanged_EventHandler(object oDataSet, ChangedEventArgs e)
        {
            OpenTrader.Data.DataSet dataset = oDataSet as OpenTrader.Data.DataSet;

            switch (e.Action)
            {
                case ChangedAction.Add:
                    dataset.Add();
                    break;

                case ChangedAction.Remove:
                    dataset.Remove();
                    break;

                case ChangedAction.Replace:
                    dataset.Save();
                    break;
            }
        }

    }
}
