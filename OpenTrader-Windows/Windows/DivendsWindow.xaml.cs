using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for DivendsWindow.xaml
    /// </summary>
    public partial class DivendsWindow : Window
    {
        Data.DataFile? dataFile;
        List<Data.Dividend> dividends = new List<Data.Dividend>();
        List<Data.Dividend> deletedDividends = new List<Data.Dividend>();

        public Data.DataFile? DataFile
        {
            get => dataFile;
            set
            {
                dataFile = value;
                dividends = Data.Dividend.GetYahooCode(dataFile.YahooCode);
                deletedDividends = new List<Data.Dividend>();
                dividends.Sort((x, y) => y.Payable.CompareTo(x.Payable));
                UpdateDataGrid();
            }
        }
        public DivendsWindow()
        {
            InitializeComponent();
        }

        private void UpdateDataGrid()
        {
            if (dataFile == null)
                return;

            DividendGrid.ItemsSource = dividends;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (dividends == null)
                return;

            var dividend = new Data.Dividend()
            {
                YahooCode = dataFile.YahooCode
            };
            dividends.Insert(0, dividend);
            UpdateDataGrid();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.dataFile == null)
                return;

            foreach (var dividend in deletedDividends)
            {
                dividend.Remove();
            }

            foreach (var dividend in dividends)
            {
                dividend.Save();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DividendGrid.SelectedItem == null)
                return;

            if (DividendGrid.SelectedItem is Data.Dividend dividend)
            {
                if (deletedDividends != null && dividends != null)
                {
                    deletedDividends.Add(dividend);
                    dividends.Remove(dividend);
                }
            }
            UpdateDataGrid();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataFile == null)
                return;

            var historical = new List<Data.Dividend>();


            if (dataFile.YahooCode.Length > 3 && dataFile.YahooCode.Substring(dataFile.YahooCode.Length - 3) == ".NZ")
            {
                historical = Data.Dividend.ReadFromNZX(dataFile.YahooCode);
            }
            else if (dataFile.YahooCode.Length > 3 && dataFile.YahooCode.Substring(dataFile.YahooCode.Length - 3) == ".AX")
            {
                historical = Data.Dividend.ReadFromDividendDates(dataFile.YahooCode);
            }


            var needsUpdating = false;
            foreach( var dividend in historical )
            {
                var found = dividends.Find(d => d.Payable == dividend.Payable);

                if (found == null)
                {
                    found = deletedDividends.Find(d => d.Payable == dividend.Payable);
                    if( found == null)
                    {
                        needsUpdating = true;
                        dividends.Add(dividend);
                    }
                    dividend.Save();
                }
            }

            if (needsUpdating)
            {
                dividends.Sort((x, y) => y.Payable.CompareTo(x.Payable));
                UpdateDataGrid();
            }
        }

        private void CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

        }


        private void DividendGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DividendGrid_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }

        override protected void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            MainWindow.DividendsWindow = null;
        }
    }
}
