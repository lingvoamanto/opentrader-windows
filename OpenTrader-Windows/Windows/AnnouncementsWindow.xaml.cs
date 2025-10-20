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
    /// Interaction logic for AnnouncementsWindow.xaml
    /// </summary>
    /// 
    public partial class AnnouncementsWindow : Window
    {
        Data.DataFile? dataFile;
        public Data.DataFile DataFile
        {
            get => dataFile;

            set
            {
                dataFile = value;
                UpdateElements();
            }

        }
        public AnnouncementsWindow()
        {
            InitializeComponent();
        }

        private async void UpdateElements()
        {
            if (DataFile != null)
            {
                await WebView.EnsureCoreWebView2Async();
                var source = @"https://www.nzx.com/companies/" + DataFile.Name + @"/announcements";
                WebView.CoreWebView2.Navigate(source);
            }
        }
    }
}
