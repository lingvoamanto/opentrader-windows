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

namespace OpenTrader
{
    /// <summary>
    /// Interaction logic for NewsWindow.xaml
    /// </summary>
    public partial class NewsWindow : Window
    {
        Data.DataFile dataFile;

        public Data.DataFile DataFile
        {
            set { dataFile = value;
                WebView.Source = new Uri(@"https://news.google.com/search?q=" + dataFile.YahooCode + "%20business%20"+dataFile.Description);
            }
        }

        public NewsWindow(Data.DataFile dataFile)
        {
            InitializeComponent();
            DataFile = dataFile;
        }
    }
}
