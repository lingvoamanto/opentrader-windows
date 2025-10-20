using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for TradeWindow.xaml
    /// </summary>
    public partial class TradeWindow : Window
    {
        Data.DataFile? dataFile;
        List<Data.Trade>? trades;
        List<Data.Trade>? deletedTrades;

        public Data.DataFile? DataFile
        {
            get => dataFile;
            set
            {
                dataFile = value;
                UpdateDataGrid();
            }
        }
        public TradeWindow()
        {
            InitializeComponent();
        }

        private void TradeGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TradeGrid_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }

        private void UpdateDataGrid()
        {
            if (dataFile == null)
                return;

            trades = Data.Trade.GetYahooCode(dataFile.YahooCode);
            trades.Sort((x, y) => x.Date.CompareTo(y.Date));
            deletedTrades = new List<Data.Trade>();

            TradeGrid.ItemsSource = new ObservableCollection<Data.Trade>(trades);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (trades == null)
                return;

            var trade = new Data.Trade()
            {
                YahooCode = dataFile.YahooCode
            };
            trades.Insert(0,trade);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.dataFile == null)
                return;

            foreach (var trade in deletedTrades)
            {
                trade.Remove();
            }

            double inPlay = 0;
            foreach (OpenTrader.Data.Trade trade in TradeGrid.ItemsSource)
            {
                trade.YahooCode = dataFile.YahooCode;
                trade.Save();
                inPlay += trade.Quantity;
            }


            MainWindow.UpdateIsTrading(dataFile.YahooCode,inPlay > float.Epsilon);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TradeGrid.SelectedItem == null)
                return;

            if( TradeGrid.SelectedItem is Data.Trade trade )
            {
                if (deletedTrades != null && trades != null)
                {
                    deletedTrades.Add(trade);
                    trades.Remove(trade);
                }
            }
        }
    }
}
