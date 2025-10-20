using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenTrader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    using OpenTrader.Data;
    using System.IO;
    using OpenTrader;
    using System.Reflection;
    using System.Security.Cryptography;
    using OpenTrader.Item;
    using System.Net.Http;
    using System.Net;

    public partial class MainWindow : Window
    {
        public static DataSets dataSets;
        static private bool isProfiling;
        static private ProfileStack profileStack;
        private volatile bool isConnected;
        public static DataFile? dataFile;
        public static Windows.JournalWindow? journalWindow;
        Windows.AnnouncementsWindow? announcementsWindow;
        public static Windows.TradeWindow? tradeWindow;
        Windows.SharesiesWindow? sharesiesWindow;
        public static NewsWindow? newsWindow;
        public static Windows.DivendsWindow? dividendsWindow;
        static Windows.CandleWindow? candleWindow;
        static TraderBook? traderBook;
        static TreeView? treeView;
        static DataSet? dataSet;
        static bool isUpdating = false;
        static object updatingLock = new object();

        System.Drawing.Font font = new System.Drawing.Font("Arial",12);

        ContextMenu dataSetMenu = new ContextMenu();
        ContextMenu dataFileMenu = new ContextMenu();

        public static bool IsProfiling { get => isProfiling; set => isProfiling = value; }

        public static Preference SharesiesUser { get; set; }
        public static Preference SharesiesPassword { get; set; }
        public static Preference StrategyPath { get; set; }
        public static Preference OpenTraderUser { get; set; }
        public static Preference OpenTraderPassword { get; set; }

        public static Windows.CandleWindow? CandleWindow
        {
            set => candleWindow = value;
        }

        public static Windows.DivendsWindow? DividendsWindow
        {
            set => dividendsWindow = value;
        }

        public static TraderBook? TraderBook
        {
            get=>traderBook;
            set => traderBook = value;
        }

        static public ProfileStack ProfileStack
        {
            get => profileStack;
        }
        public static DataSets DataSets
        {
            get
            {
                if (dataSets == null)
                {
                    dataSets = DataSet.GetAll();
                }
                return dataSets;
            }
        }

        private TraderBook ActiveBook
        {
            get
            {
                int pageno = Library.SelectedIndex;
                if (pageno != -1)
                {
                    return (Library.SelectedItem as TabItem).Content as TraderBook;
                }
                else
                {
                    return null;
                }
            }
        }

        public bool IsConnected
        {
            get { return isConnected; }
            set { isConnected = value; }
        }

        public void CheckForInternetConnection()
        {
            try
            {
                IsConnected = true;

                /*
                Ping ping = new Ping();
                PingOptions options = new PingOptions();
                options.DontFragment = true;
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data);
                int timeout = 120;
                PingReply reply = ping.Send("google.com", timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                */
                using (var client = new System.Net.WebClient())
                {
                    using (client.OpenRead("http://google.com/generate_204"))
                    {
                        IsConnected = true;
                    }
                }
                // }
            }
            catch (System.Exception e)
            {
                DebugHelper.WriteLine(e);
                IsConnected = false;
            }
        }
        public MainWindow()
        {
            Preference.Defaults.Add((name: Preference.Machine, value: Guid.NewGuid().ToString()));
            Preference.Defaults.Add((name: Preference.LastTransaction, value: "0"));
            Preference.Defaults.Add((name: Preference.LastTimeStamp, value: "1970-01-01 00:00:00"));


            SharesiesUser = Preference.Get("SharesiesUser");
            SharesiesPassword = Preference.Get("SharesiesPassword");
            StrategyPath = Preference.Get("StrategyPath");
            OpenTraderUser = Preference.Get("OpenTraderUser");
            OpenTraderPassword = Preference.Get("OpenTraderPassword");

            InitializeComponent();

            GetYahooCookies();

            var startTimeSpan = System.TimeSpan.Zero;
            var periodTimeSpan = System.TimeSpan.FromMinutes(10);

            var timer = new System.Threading.Timer(async (e) =>
            {
                // var thread = System.Threading.Thread.CurrentThread;
                // thread.Priority = System.Threading.ThreadPriority.Lowest;

                CheckForInternetConnection();
                if (IsConnected)
                {

                    if (isUpdating)
                        return;

                    lock (updatingLock)
                    {
                        isUpdating = true;
                    }

                    try
                    {
                        // if( updatedCrumb ) UpdateDataSets(instruments);
                    }
                    catch (Exception exception)
                    {
                        DebugHelper.WriteLine(exception);
                    }

                    Dividend.UpdateFromNZX();

                    // Make sure you've moved readhistorical in the updatedatasets call
                    await Transaction.SyncUp();
                    await Transaction.SyncDown();

                    isUpdating = false;
                }
            }, null, startTimeSpan, periodTimeSpan);



            treeView = TreeView;



        }


        private void TreeView_Loaded(object sender, RoutedEventArgs e)
        {
            var alertImage = new BitmapImage();
            alertImage.BeginInit();
            alertImage.UriSource = new Uri("pack://application:,,,/OpenTrader;component/images/Fishing net.png");
            alertImage.EndInit();

            var propertiesImage = new BitmapImage();
            propertiesImage.BeginInit();
            propertiesImage.UriSource = new Uri("pack://application:,,,/OpenTrader;component/images/Properties.png");
            propertiesImage.EndInit();

            var candleImage = new BitmapImage();
            candleImage.BeginInit();
            candleImage.UriSource = new Uri("pack://application:,,,/OpenTrader;component/images/Candlestick.png");
            candleImage.EndInit();

            var historicalImage = new BitmapImage();
            historicalImage.BeginInit();
            historicalImage.UriSource = new Uri("pack://application:,,,/OpenTrader;component/images/History.png");
            historicalImage.EndInit();

            var currentImage = new BitmapImage();
            currentImage.BeginInit();
            currentImage.UriSource = new Uri("pack://application:,,,/OpenTrader;component/images/Now.png");
            currentImage.EndInit();

            var addImage = new BitmapImage();
            addImage.BeginInit();
            addImage.UriSource = new Uri("pack://application:,,,/OpenTrader;component/images/Add.png");
            addImage.EndInit();

            var dsReadCurrent = new MenuItem();
            dsReadCurrent.Header = "Read current";
            dsReadCurrent.Click += ReadCurrentDataSet_Click;
            dsReadCurrent.Icon = new Image() { Source = currentImage};
            dataSetMenu.Items.Add(dsReadCurrent);

            var candleData = new MenuItem();
            candleData.Header = "Candle data";
            candleData.Click += CandleDataButton_Click;
            candleData.Icon = new Image() { Source = candleImage };
            dataSetMenu.Items.Add(candleData);

            var dsReadHistorical = new MenuItem();
            dsReadHistorical.Header = "Read historical";
            dsReadHistorical.Click += ReadHistoricalDataSet_Click;
            dsReadHistorical.Icon = new Image() { Source = historicalImage };
            dataSetMenu.Items.Add(dsReadHistorical);

            var dsProperties = new MenuItem();
            dsProperties.Header = "Properties";
            dsProperties.Click += DataSetProperties_Click;
            dsProperties.Icon = new Image() { Source = propertiesImage };
            dataSetMenu.Items.Add(dsProperties);

            var dsAdd = new MenuItem();
            dsAdd.Header = "Add data file";
            dsAdd.Click += AddDataFile_Click;
            dsAdd.Icon = new Image() { Source = addImage };
            dataSetMenu.Items.Add(dsAdd);

            var alert = new MenuItem();
            alert.Header = "Alert";
            alert.Click += Alert_Click;
            alert.Icon = new Image() { Source = alertImage };
            dataFileMenu.Items.Add(alert);

            var dfProperties = new MenuItem();
            dfProperties.Header = "Properties";
            dfProperties.Click += DataFileProperties_Click;
            dfProperties.Icon = new Image() { Source = propertiesImage };
            dataFileMenu.Items.Add(dfProperties);

            var dfEditData = new MenuItem();
            dfEditData.Header = "Edit data";
            dfEditData.Click += DataFileEditData_Click;
            dfEditData.Icon = new Image() { Source = propertiesImage };
            dataFileMenu.Items.Add(dfEditData);

            if (sender is TreeView treeView)
            {
                foreach (var dataSet in DataSets)
                {
                    var item = new TreeViewItem();
                    item.Header = dataSet.Name;
                    item.Tag = dataSet;
                    item.ContextMenu = dataSetMenu;

                    foreach (var dataFile in dataSet.DataFiles)
                    {
                        var child = new TreeViewItem();
                        child.Header = dataFile.Title;
                        child.Foreground = dataFile.IsTrading ? Brushes.Green : dataFile.Alert ? Brushes.Brown : dataFile.Watching ? Brushes.Blue : Brushes.Black;
                        child.Tag = dataFile;
                        child.ContextMenu = dataFileMenu;
                        item.Items.Add(child);                      
                    }

                    treeView.Items.Add(item);
                }
            }
        }

        void DataSetProperties_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (menuItem.Tag is DataSet ds)
                {
                    dataSet = ds;
                }
            }

            if (dataSet != null)
            {
                var pw = new Windows.DataSetWindow(dataSet);
                pw.Left = Left + (ActualWidth - pw.Width) / 2;
                pw.Top = Top + 78;
                pw.ShowDialog();
            }
        }

        void DataFileProperties_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if ( menuItem.Tag is DataFile df)
                {
                    dataFile = df;
                }
             }

            if (dataFile != null)
            {
                var pw = new Windows.DataFileWindow(dataFile);
                pw.Left = Left + (ActualWidth - pw.Width) / 2;
                pw.Top = Top + 78;
                pw.ShowDialog();
            }
        }

        void DataFileEditData_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (menuItem.Tag is DataFile df)
                {
                    dataFile = df;
                }
            }

            if (dataFile != null)
            {
                var pw = new Windows.BarsWindow();
                pw.DataFile = dataFile;
                pw.Left = Left + (ActualWidth - pw.Width) / 2;
                pw.Top = Top + 78;
                pw.ShowDialog();
            }
        }


        private void ReadAllDataFiles_Click(object sender, RoutedEventArgs e)
        {
            var originalCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            var yahooReader = new YahooReader();
            var dataFiles = DataFile.GetAll();
            Parallel.ForEach(dataFiles, df =>
            {
                try
                {
                    df.Initialise();
                    df.ReadBarsFromFile();
                    if (df.bars.Date.Count > 0)
                    {
                        if (df.LastUpdated < System.DateTime.Now.AddMinutes(-15))
                        {
                            yahooReader.ReadHistorical(df, df.bars.Date[^1].AddDays(-5), DateTime.Now, true);
                        }
                    }
                    else
                        yahooReader.ReadHistorical(df, df.YahooStart, DateTime.Now, true);
                    df.LastUpdated = DateTime.Now;
                    df.Save();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });

            Mouse.OverrideCursor = originalCursor;
        }

        private void ReadHistoricalDataSet_Click(object sender, RoutedEventArgs e)
        {
            var originalCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            if( sender is MenuItem menuItem )
            {
                if (menuItem.Tag is DataSet ds)
                    dataSet = ds;
            }

            var checkDate = DateTime.Now.AddMinutes(-5);

            if (dataSet != null)
            {
                var yahooReader = new YahooReader();
                var dataFiles = dataSet.DataFiles.Where(df => df.LastUpdated < checkDate).ToList();
                dataFiles.Sort((a,b)=>a.LastUpdated.CompareTo(b.LastUpdated));
                var count = dataFiles.Count();
                Parallel.ForEach(dataFiles, (df,state) =>
                 {
                     if (df.bars.Count > 0)
                     {
                         df.Read();
                     }

                     if (df.bars.Count > 0)
                     {
                         var result = yahooReader.ReadHistorical(df, df.bars.Date[^1].AddDays(-5), DateTime.Now, true);
                         if (result == ReadResult.Success || result == ReadResult.NoTBody)
                         {
                             df.LastUpdated = DateTime.Now;
                             df.Save();
                         }
                         else
                         {
                             state.Stop();
                         }
                     }
                     else
                     {
                         var result = yahooReader.ReadHistorical(df, df.YahooStart, DateTime.Now, true);
                         if (result == ReadResult.Success || result == ReadResult.NoTBody)
                         {
                             df.LastUpdated = DateTime.Now;
                             df.Save();
                         }
                         else
                         {
                             state.Stop();
                         }
                     }
                 });
            }

            Mouse.OverrideCursor = originalCursor;
        }

        private void ReadCurrentDataSet_Click(object sender, RoutedEventArgs e)
        {
            if (dataSet != null)
            {
                var yahooApi = new YahooApi();
                yahooApi.ReadCurrent(dataSet);
            }
        }

        private void AddDataFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if( menuItem.Tag is DataSet ds)
                {
                    dataSet = ds;
                }
            }

            if (dataSet != null)
            {
                var df = new DataFile(dataSet.DataFiles,"","");

                var pw = new Windows.DataFileWindow(df);
                pw.Left = Left + (ActualWidth - pw.Width) / 2;
                pw.Top = Top + 78;
                pw.ShowDialog();
            }
        }

        private void Alert_Click(object sender, RoutedEventArgs e)
        {
            if (traderBook == null)
                return;

            YahooReader yahooReader = new YahooReader();

            var originalCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            var lastBar = dataFile.bars.Count - 1;
            if (lastBar < 0)
            {
                dataFile.Alert = false;
                return;
            }

            /*
            bool didRead = false;
            try
            {
                var startDate = df.bars.Count == 0 ? df.YahooStart : df.bars.Date[Math.Min(lastBar - 5, 0)];
                yahooReader.ReadHistorical(df, startDate, DateTime.Now, true);
            }
            catch 
            {
                System.Diagnostics.Debug.Print(df.Name + " had an error");
            }
            */

            try
            {
                dataFile.Alert = traderBook.TraderScript.Alert(dataFile);
            }
            catch
            {
                dataFile.Alert = false;
                System.Diagnostics.Debug.Print(dataFile.Description + " had an error");
            }

            if (dataFile.Alert)
            {
                System.Diagnostics.Debug.Print(dataFile.Description + " has an alert");
            }

            UpdateForeground();
            Mouse.OverrideCursor = originalCursor;
        }

        public static void UpdateIsTrading(string yahooCode, bool isTrading)
        {
            if (treeView == null || treeView.Items == null)
                return;

            foreach (TreeViewItem? setItem in treeView.Items)
            {
                if (setItem == null || setItem.Items == null)
                    continue;

                foreach (TreeViewItem? child in setItem.Items)
                {
                    if (child == null)
                        continue;
                    if (child.Tag is DataFile dataFile)
                    {
                        if (dataFile.YahooCode == yahooCode)
                        {
                            dataFile.IsTrading = isTrading;
                            dataFile.Save();
                            child.Foreground = isTrading ? Brushes.Green : dataFile.Alert ? Brushes.Brown : dataFile.Watching ? Brushes.Blue : Brushes.Black;
                        }
                    }
                }
            }
        }

        private void TreeView_SelectedItemChanged(object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            var item = TreeView.SelectedItem as TreeViewItem;
            object? o_value = item == null ? null : item.Tag;

            if (o_value != null)
            {
                // menuitemFileUpdateData.Visibility = Visibility.Hidden;
                if (o_value.GetType() == typeof(DataSet))
                {
                    dataSet = o_value as DataSet;
                    if (candleWindow != null)
                        candleWindow.DataSet = dataSet;
                }
                else if (o_value.GetType() == typeof(DataFile))
                {
                    System.Windows.Input.Cursor storedCursorType = this.Cursor;
                    this.Cursor = System.Windows.Input.Cursors.Wait;

                    dataFile = (DataFile)o_value;
                    dataSet = dataFile.DataSet;

                    if (dataFile.bars.Count == 0)
                    {

                        dataFile.Read();
                        var reader = new YahooReader();
                    }

                    if (IsConnected)
                    {
                        YahooReader reader = new YahooReader();
                        int count = dataFile.bars.Count;

                        if (count == 0)
                        {
                            reader.ReadHistorical(dataFile, dataFile.YahooStart, System.DateTime.Now);
                            // dataFile.Write();
                            var task = System.Threading.Tasks.Task.Run(() =>
                            {
                                dataFile.Write();
                            });
                            dataFile.LastUpdated = System.DateTime.Now;
                            dataFile.Save();
                        } 
                        else if (dataFile.LastUpdated < System.DateTime.Now.AddMinutes(-15))
                        {
                            DateTime startDate;
                            if (count > 2)
                            {
                                startDate = dataFile.bars.Date[dataFile.bars.Count - 2];
                            }
                            else
                            {
                                startDate = dataFile.bars.Date[0];
                            }
                            reader.ReadHistorical(dataFile, startDate, System.DateTime.Now);
                            // dataFile.Write();
                            var task = System.Threading.Tasks.Task.Run(() =>
                            {
                                dataFile.Write();
                            });
                            dataFile.LastUpdated = System.DateTime.Now;
                            dataFile.Save();
                            dataFile.UpdateWeeks(startDate);

                            /*
                            if (dataFile.DataSet.Exchange == "NZX")
                            {
                                System.Threading.Tasks.Task.Run(() =>
                                {
                                    Fundamental.UpdateFromNZX(dataFile.YahooCode);
                                });
                            }
                            */
                        }
                        dataFile.OnChanged(null);

                        if (journalWindow != null)
                            journalWindow.DataFile = dataFile;
                        if (announcementsWindow != null)
                            announcementsWindow.DataFile = dataFile;
                        if (tradeWindow != null)
                            tradeWindow.DataFile = dataFile;
                        if (sharesiesWindow != null)
                            sharesiesWindow.DataFile = dataFile;
                        if (dividendsWindow != null)
                            dividendsWindow.DataFile = dataFile;
                        if (newsWindow != null)
                            newsWindow.DataFile = dataFile;
                    }

                    this.Cursor = storedCursorType;

                    if (ActiveBook != null)
                        ActiveBook.CursorChanged(dataFile);

                }
                else
                {
                    // menuitemFileUpdateData.Visibility = Visibility.Visible;
                }
            }
        }

        void StrategyNew(object sender, RoutedEventArgs e)
        {
            var scriptFile = new ScriptFile()
            {
                Language = OpenTrader.Language.OpenScript,
                Name = "NewScript",
                Code = "",
                Guid = Guid.NewGuid(),
                Modified = DateTime.Now,
            };

            TraderBook traderBook = new TraderBook(this, scriptFile);
            TraderBook = traderBook;
            traderBook.Library = Library;
            TabItem tabItem = new TabItem()
            {
                Content = (TabControl)traderBook,
                Header = traderBook.TitleBox
            };
            traderBook.TabItem = tabItem;
            int PageNo = Library.Items.Add(tabItem);
            Library.SelectedIndex = PageNo;
        }

        void StrategyOpened(object sender)
        {
            if (sender is Item.ScriptItem scriptItem)
            {
                var scriptFile = scriptItem.ScriptFile;
                TraderBook traderBook = new TraderBook(this, scriptFile);
                TraderBook = traderBook;
                traderBook.Library = Library;
                TabItem tabItem = new TabItem()
                {
                    Content = (TabControl)traderBook,
                    Header = traderBook.TitleBox
                };
                traderBook.TabItem = tabItem;
                int PageNo = Library.Items.Add(tabItem);
                Library.SelectedIndex = PageNo;
            }
        }

        private void StrategyOpen(object sender, RoutedEventArgs e)
        {
            var window = new Windows.OpenFileWindow();
            window.OnSave = StrategyOpened;
            window.ShowDialog();
        }

        /*
        private void StrategyOpen(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog()
            {
                Title = "Open Strategy",
                CheckFileExists = true,
                InitialDirectory = StrategyPath.Value,
                Multiselect = false
            };

            ofd.ShowDialog();

            if (ofd.FileName != "")
            {
                TraderBook traderBook = new TraderBook(this, ofd.FileName);
                TraderBook = traderBook;
                traderBook.Library = Library;
                TabItem tabItem = new TabItem()
                {
                    Content = (TabControl)traderBook,
                    Header = traderBook.TitleBox
                };
                traderBook.TabItem = tabItem;
                int PageNo = Library.Items.Add(tabItem);
                Library.SelectedIndex = PageNo;
            }
        }
        */

        void StrategySave(object o, RoutedEventArgs e)
        {
            var tabItem = Library.SelectedItem as TabItem;
            var traderBook = tabItem.Content as TraderBook;
            traderBook.ScriptFile.Name = traderBook.Title;
            traderBook.ScriptFile.Code = traderBook.EditorPage.Text;
            traderBook.ScriptFile.Save();
        }

        /*
        void StrategySaveAs(object o, RoutedEventArgs e)
        {
            var sfd = new System.Windows.Forms.SaveFileDialog()
            {
                Title = "Strategy",
                InitialDirectory = "D:\\Dropbox\\Trading\\TradingScripts"
            };

            sfd.ShowDialog();

            if (sfd.FileName != "")
            {

                // ActivePage.FileName = sfd.FileName;
                var traderBook = Library.SelectedItem as TraderBook;
                if (traderBook != null)
                {
                    traderBook.FileName = sfd.FileName;
                    traderBook.Save();
                }
            }
        }
        */

        public void ReadUpdate(DataFile dataFile)
        {
            if (IsConnected)
            {
                var reader = new YahooReader();
                var api = new YahooApi();
                if (dataFile.bars.Count == 0)
                {
                    reader.ReadHistorical(dataFile, dataFile.YahooStart, System.DateTime.Now, true);
                    dataFile.Write();
                    dataFile.HasReadHistorical = true;
                }
                else
                {
                    DateTime startDate = dataFile.bars.Date[dataFile.bars.Count - 1];
                    if (startDate.Date == DateTime.Now.Date)
                        api.ReadCurrent(dataFile);
                    else
                        reader.ReadHistorical(dataFile, startDate, System.DateTime.Now, true);
                    dataFile.Write();
                    dataFile.UpdateWeeks(startDate);
                }
                dataFile.OnChanged(null);
            }
        }

        private void SharesiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (sharesiesWindow == null)
            {
                sharesiesWindow = new Windows.SharesiesWindow(dataFile);
                sharesiesWindow.Show();
            }
            else
            {
                sharesiesWindow.Close();
                sharesiesWindow = null;
            }
        }

        private void CandleDataButton_Click(object sender, RoutedEventArgs e)
        {
            if( sender is MenuItem menuItem)
            {
                if( menuItem.Tag is DataSet ds)
                {
                    if (ds != null)
                        dataSet = ds;
                }
            }

            if (dataSet == null)
                return;


            if (candleWindow == null)
            {
                candleWindow = new Windows.CandleWindow();
                candleWindow.DataSet = dataSet;
                candleWindow.Show();
            }
            else
            {
                candleWindow.Close();
                candleWindow = null;
            }
        }

        private void DividendsButton_Click(object sender, RoutedEventArgs e)
        {
            if (dividendsWindow == null)
            {
                dividendsWindow = new Windows.DivendsWindow();
                dividendsWindow.DataFile = dataFile;
                dividendsWindow.Show();
            }
            else
            {
                dividendsWindow.Close();
                dividendsWindow = null;
            }
        }

        private void JournalButton_Click(object sender, RoutedEventArgs e)
        {
            if (journalWindow == null)
            {
                journalWindow = new Windows.JournalWindow();
                journalWindow.DataFile = dataFile;
                journalWindow.Show();
            }
            else
            {
                journalWindow.Close();
                journalWindow = null;
            }
        }

        private void AnnouncementsButton_Click(object sender, RoutedEventArgs e)
        {
            if (announcementsWindow == null)
            {
                announcementsWindow = new Windows.AnnouncementsWindow();
                announcementsWindow.DataFile = dataFile;
                announcementsWindow.Show();
            }
            else
            {
                announcementsWindow.Close();
                announcementsWindow = null;
            }
        }

        private void TradeButton_Click(object sender, RoutedEventArgs e)
        {
            if (tradeWindow == null)
            {
                tradeWindow = new Windows.TradeWindow();
                tradeWindow.DataFile = dataFile;
                tradeWindow.Show();
            }
            else
            {
                tradeWindow.Close();
                tradeWindow = null;
            }
        }

        private void OptimiseButton_Click(object sender, RoutedEventArgs e)
        {
            var optimisation = new Optimisation(traderBook);
            optimisation.Optimise();
        }

        private void CalibrateButton_Click(object sender, RoutedEventArgs e)
        {
            var originalCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            if ( dataSet != null)
            {

                Parallel.ForEach(dataSet.DataFiles,df=>
                {
                    if (df.bars.Close.Count < 1)
                    {
                        try
                        {
                            df.Read(); ;
                        }
                        catch { return; }
                    }
                });
    
                var calibration = new Calibration(dataSet);
                calibration.Calibrate();
            }
            Mouse.OverrideCursor = originalCursor;
        }

        private void AlertsButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataSet == null || traderBook == null || traderBook.TraderScript == null)
                return;

            var count = Environment.ProcessorCount;

            YahooReader yahooReader = new YahooReader();

            var originalCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = count
            };

            Parallel.ForEach(dataSet.DataFiles,options,df=>
            {
                if (df.bars.Count == 0)
                    return;
                var lastBar = df.bars.Count - 1;
                if (lastBar < 0)
                {
                    df.Alert = false;
                    return;
                }

                /*
                bool didRead = false;
                try
                {
                    var startDate = df.bars.Count == 0 ? df.YahooStart : df.bars.Date[Math.Min(lastBar - 5, 0)];
                    yahooReader.ReadHistorical(df, startDate, DateTime.Now, true);
                }
                catch 
                {
                    System.Diagnostics.Debug.Print(df.Name + " had an error");
                }
                */

                try
                {
                    df.Alert = traderBook.TraderScript.Alert(df);
                }
                catch
                {
                    df.Alert = false;
                    System.Diagnostics.Debug.Print(df.Description + " had an error");
                }

                if (df.Alert)
                {
                    System.Diagnostics.Debug.Print(df.Description + " has an alert");
                }
            });
            UpdateForeground();
            Mouse.OverrideCursor = originalCursor;
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            var preferencesWindow = new Windows.PreferencesWindow();
            preferencesWindow.ShowDialog();
        }

        private void TrendlineGuids_Click(object sender, RoutedEventArgs e)
        {
            TrendLine.UpdateGuids();
        }

        private async void UploadTrendlines_Click(object sender, RoutedEventArgs e)
        {
            var trendLines = TrendLine.GetAll();
            var machine = Preference.Get(Preference.Machine);
            foreach (var trendLine in trendLines)
            {
                var transaction = new Transaction()
                {
                    Data = trendLine.Serialise(),
                    Guid = trendLine.Guid,
                    Machine = machine.Value,
                    FileName = TrendLine.TableName,
                    Method = "update"
                };
                var result = await SoapClient.AddTransaction(transaction);
            }

        }

        private void TradeGuids_Click(object sender, RoutedEventArgs e)
        {
            Trade.UpdateGuids();
        }

        private async void UploadTrades_Click(object sender, RoutedEventArgs e)
        {
            var trades = Trade.GetAll();
            var machine = Preference.Get(Preference.Machine);
            foreach (var trade in trades)
            {
                var transaction = new Transaction()
                {
                    Data = trade.Serialise(),
                    Guid = trade.Guid,
                    Machine = machine.Value,
                    FileName = Trade.TableName,
                    Method = "update"
                };
                var result = await SoapClient.AddTransaction(transaction);
            }

        }

        private void DividendGuids_Click(object sender, RoutedEventArgs e)
        {
            Dividend.UpdateGuids();
        }

        private async void UploadDividends_Click(object sender, RoutedEventArgs e)
        {
            var dividends = Dividend.GetAll();
            var machine = Preference.Get(Preference.Machine);
            foreach (var dividend in dividends)
            {
                var transaction = new Transaction()
                {
                    Data = dividend.Serialise(),
                    Guid = dividend.Guid,
                    Machine = machine.Value,
                    FileName = Dividend.TableName,
                    Method = "update"
                };
                var results = await SoapClient.AddTransaction(transaction);
                var result = results.First();
            }

        }

        private void AnnotationGuids_Click(object sender, RoutedEventArgs e)
        {
            Data.Annotation.UpdateGuids();
        }

        private async void UploadAnnotations_Click(object sender, RoutedEventArgs e)
        {
            var annotations = Data.Annotation.GetAll();
            var machine = Preference.Get(Preference.Machine);
            foreach (var annotation in annotations)
            {
                var transaction = new Transaction()
                {
                    Data = annotation.Serialise(),
                    Guid = annotation.Guid,
                    Machine = machine.Value,
                    FileName = Data.Annotation.TableName,
                    Method = "update"
                };
                var results = await SoapClient.AddTransaction(transaction);
                var result = results.First();
            }

        }

        private void JournalEntryGuids_Click(object sender, RoutedEventArgs e)
        {
            JournalEntry.UpdateGuids();
        }

        private async void UploadJournalEntries_Click(object sender, RoutedEventArgs e)
        {
            var items = JournalEntry.GetAll();
            var machine = Preference.Get(Preference.Machine);
            foreach (var item in items)
            {
                var transaction = new Transaction()
                {
                    Data = item.Serialise(),
                    Guid = item.Guid,
                    Machine = machine.Value,
                    FileName = JournalEntry.TableName,
                    Method = "update"
                };
                var results = await SoapClient.AddTransaction(transaction);
                var result = results.First();
            }
        }

        private void DataSetGuids_Click(object sender, RoutedEventArgs e)
        {
            DataSet.UpdateGuids();
        }

        private async void UploadDataSets_Click(object sender, RoutedEventArgs e)
        {
            var items = DataSet.GetAll();
            var machine = Preference.Get(Preference.Machine);
            foreach (var item in items)
            {
                var transaction = new Transaction()
                {
                    Data = item.Serialise(),
                    Guid = item.Guid,
                    Machine = machine.Value,
                    FileName = DataSet.TableName,
                    Method = "update"
                };
                var results = await SoapClient.AddTransaction(transaction);
                var result = results.First();
            }
        }

        private void CandleDataGuids_Click(object sender, RoutedEventArgs e)
        {
            CandleData.UpdateGuids();
        }

        private async void UploadCandleData_Click(object sender, RoutedEventArgs e)
        {
            var items = CandleData.GetAll();
            var machine = Preference.Get(Preference.Machine);
            foreach (var item in items)
            {
                var transaction = new Transaction()
                {
                    Data = item.Serialise(),
                    Guid = item.Guid,
                    Machine = machine.Value,
                    FileName = CandleData.TableName,
                    Method = "update"
                };
                var results = await SoapClient.AddTransaction(transaction);
                var result = results.First();
            }
        }

        private void DataFileGuids_Click(object sender, RoutedEventArgs e)
        {
            DataFile.UpdateGuids();
        }

        private async void UploadDataFile_Click(object sender, RoutedEventArgs e)
        {
            var items = DataFile.GetAll();
            var machine = Preference.Get(Preference.Machine);
            foreach (var item in items)
            {
                var transaction = new Transaction()
                {
                    Data = item.Serialise(),
                    Guid = item.Guid,
                    Machine = machine.Value,
                    FileName = DataFile.TableName,
                    Method = "update"
                };
                var results = await SoapClient.AddTransaction(transaction);
                var result = results.First();
            }
        }

        public void UpdateForeground()
        {
            if (TreeView == null || TreeView.Items == null)
                return;

            foreach (TreeViewItem? setItem in TreeView.Items)
            {
                if (setItem == null || setItem.Items == null)
                    continue;

                foreach (TreeViewItem? child in setItem.Items)
                {
                    if (child == null)
                        continue;
                    if (child.Tag is DataFile df)
                    {
                        if (df.Alert)
                            child.Foreground = Brushes.Brown;

                        child.Foreground = df.IsTrading ? Brushes.Green : df.Alert ? Brushes.Brown : df.Watching ? Brushes.Blue : Brushes.Black;
                        child.UpdateLayout();
                    }
                }
            }
        }

        public static async Task GetYahooCookies()
        {
            var browser = new Microsoft.Web.WebView2.WinForms.WebView2();
            // browser.Source = new System.Uri("https://finance.yahoo.com/QUOTE/AAPL");
            await browser.EnsureCoreWebView2Async();
            browser.NavigateToString("https://finance.yahoo.com/QUOTE/AAPL");
            var cookies = await browser.CoreWebView2.CookieManager.GetCookiesAsync("https://yahoo.com");
            YahooReader.Cookies.Clear();
            foreach (var cookie in cookies)
            {
                var yahooCookie = new System.Net.Cookie()
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Expires = cookie.Expires,
                    Path = cookie.Path,
                    Domain = cookie.Domain,
                };
                YahooReader.Cookies.Add(yahooCookie);
            }
        }
    }
}
