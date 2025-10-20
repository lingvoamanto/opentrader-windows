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
    /// Interaction logic for SharesiesWindow.xaml
    /// </summary>
    public partial class SharesiesWindow : Window
    {
        Data.DataFile? dataFile;
        Sharesies sharesies;
        string source;
        public Data.DataFile DataFile
        {
            get => dataFile;

            set
            {
                dataFile = value;
                UpdateElements();
            }

        }
        public SharesiesWindow(Data.DataFile? dataFile)
        {
            InitializeComponent();

            this.dataFile = dataFile;
            WebView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
             
            Authenticate();
        }

        private void CoreWebView2_WebResourceRequested(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs e)
        {
            e.Request.Headers.SetHeader("set-cookie", sharesies.CookieString);
        }


        private void WebView_CoreWebView2InitializationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            WebView.CoreWebView2.AddWebResourceRequestedFilter(source, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
            WebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
        }


        private async void Authenticate()
        {
            sharesies = new Sharesies();

            string json = await sharesies.Authenticate(MainWindow.SharesiesUser.Value, MainWindow.SharesiesPassword.Value);
            string check = await sharesies.Check();
            UpdateElements();
        }

        private async void UpdateElements()
        {
            if (dataFile != null)
            {
                await WebView.EnsureCoreWebView2Async();
                source = @"https://app.sharesies.com/invest/" + DataFile.Name.ToLower();
                WebView.CoreWebView2.Navigate(source);
            }
            else
            {
                await WebView.EnsureCoreWebView2Async();
                source = @"https://app.sharesies.com/portfolio";
                WebView.CoreWebView2.Navigate(source);
            }
        }
    }
}
