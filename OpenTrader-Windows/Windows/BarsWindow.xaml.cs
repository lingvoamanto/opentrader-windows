using OpenTrader.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace OpenTrader.Windows
{

    /// <summary>
    /// Interaction logic for BarsWindow.xaml
    /// </summary>
    public partial class BarsWindow : Window
    {
        Data.DataFile? dataFile;
        List<Data.Trade>? trades;
        List<Data.Trade>? deletedTrades;
        List<Bar>? bars;

        public Data.DataFile? DataFile
        {
            get => dataFile;
            set
            {
                dataFile = value;
                UpdateDataGrid();
            }
        }

        public BarsWindow()
        {
            InitializeComponent();
        }

        private void UpdateDataGrid()
        {
            if (dataFile == null)
                return;

            trades = Data.Trade.GetYahooCode(dataFile.YahooCode);
            trades.Sort((x, y) => x.Date.CompareTo(y.Date));
            deletedTrades = new List<Data.Trade>();

            bars = Bar.GetEquals("YahooCode", dataFile.YahooCode);

            BarsGrid.ItemsSource = new ObservableCollection<Bar>(bars);
        }

        private void BarsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BarsGrid_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }
    }
}
