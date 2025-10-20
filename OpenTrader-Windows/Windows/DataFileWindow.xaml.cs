using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenTrader.Windows
{
    /// <summary>
    /// Interaction logic for DataFileWindow.xaml
    /// </summary>
    public partial class DataFileWindow : Window
    {
        Data.DataFile dataFile;
        public DataFileWindow(Data.DataFile dataFile)
        {
            InitializeComponent();
            this.dataFile = dataFile;

            TradingNotes.Text = dataFile.TradingNotes ?? "";

            DataSetCombo.Items.Clear();
            int selectedIndex = 0;
            int index = 0;
            foreach (var dataset in MainWindow.dataSets)
            {
                DataSetCombo.Items.Add(dataset);
                if (dataset == dataFile.DataSet)
                {
                    selectedIndex = index;
                }
                index++;
            }
            DataSetCombo.SelectedIndex = selectedIndex;

            Name.Text = dataFile.Name ?? "";
            YahooCode.Text = dataFile.YahooCode ?? "";
            Start.SelectedDate = dataFile.YahooStart;
            Description.Text = dataFile.Description ?? "";
            Watching.IsChecked = dataFile.Watching;
            WatchAmount.Text = dataFile.WatchAmount.ToString();

            if (dataFile.Image != null && dataFile.Image.Length != 0)
            {
                var bitmapImage = new BitmapImage();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(dataFile.Image))
                {
                    ms.Seek(0, System.IO.SeekOrigin.Begin);

                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();

                    Image.Source = bitmapImage; // Put it in the bar
                    Image.Width = bitmapImage.Width * 75 / bitmapImage.Height;
                }

            }
            else
            {
                Image.Source = null;
                Image.Width = 75;
            }
        }

        void Save_Click(object sender, RoutedEventArgs e)
        {
            if (DataSetCombo.SelectedItem != dataFile.DataSet)
            {
                dataFile.DatasetGuid = (DataSetCombo.SelectedItem as Data.DataSet).Guid;
            }
            dataFile.Name = Name.Text;
            dataFile.YahooCode = YahooCode.Text ;
            if (Start.SelectedDate.HasValue)
                dataFile.YahooStart = Start.SelectedDate.Value;
            dataFile.Description = Description.Text;
            if (Watching.IsChecked.HasValue)
                dataFile.Watching = Watching.IsChecked.Value;

            var watchAmount = dataFile.WatchAmount;
            if (double.TryParse(WatchAmount.Text, out watchAmount))
            {
                dataFile.WatchAmount = watchAmount;
            }

            dataFile.TradingNotes = TradingNotes.Text;

            dataFile.Save();
            this.Close();
        }

        void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
