using System;
using System.Collections.Generic;
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
    /// Interaction logic for CandleWindow.xaml
    /// </summary>
    public partial class CandleWindow : Window
    {
        Data.DataSet? dataSet;
        List<Data.CandleData> candleData = new List<Data.CandleData>();
        public Data.DataSet? DataSet
        {
            get => dataSet;
            set
            {
                dataSet = value;
                if (dataSet != null)
                {
                    candleData = Data.CandleData.GetDataSet(dataSet.Guid);
                    candleData.Sort((x, y) => x.Name.CompareTo(y.Name));
                }
                else
                {
                    candleData = new List<Data.CandleData>();
                }
                UpdateDataGrid();
            }
        }
        public CandleWindow()
        {
            InitializeComponent();
        }
        private void UpdateDataGrid()
        {
            if (dataSet == null)
                return;

            CandleGrid.ItemsSource = candleData;
        }

        override protected void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            MainWindow.CandleWindow = null;
        }
    }
}
