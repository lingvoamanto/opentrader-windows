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
    /// Interaction logic for DataSetWindow.xaml
    /// </summary>
    public partial class DataSetWindow : Window
    {
        Data.DataSet dataSet;

        public DataSetWindow(Data.DataSet dataSet)
        {
            InitializeComponent();

            this.dataSet = dataSet;

            Name.Text = dataSet.mName ?? "";
            YahooPrefix.Text = dataSet.mYahooPrefix;
            YahooSuffix.Text = dataSet.mYahooSuffix;
            Exchange.Text = dataSet.mExchange;
            YahooIndex.Text = dataSet.mYahooIndex;
        }

        void Save_Click(object sender, RoutedEventArgs e)
        {
            dataSet.mName = Name.Text;
            dataSet.mYahooPrefix = YahooPrefix.Text;
            dataSet.mYahooSuffix = YahooSuffix.Text;
            dataSet.mExchange = Exchange.Text;
            dataSet.mYahooIndex = YahooIndex.Text;

            dataSet.Save();
            this.Close();
        }

        void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
