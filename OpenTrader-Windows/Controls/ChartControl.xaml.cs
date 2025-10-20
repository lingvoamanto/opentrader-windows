using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace OpenTrader.Controls
{
    /// <summary>
    /// Interaction logic for ChartControl.xaml
    /// </summary>
    public partial class ChartControl : UserControl
    {
        WPFChart chart;
        TraderBook traderBook;
        double rightMargin = 50;

        bool isWebViewReady = false;

        ChartType chartType;
        ChartControl weekControl;
        WeekWindow? weekWindow;

        public ChartControl WeekControl
        {
            get => weekControl;
        }

        public double CandleWidth
        {
            get => chart.candleWidth;
            set => chart.candleWidth = value;
        }

        TraderBook TraderBook
        {
            get => traderBook;
            set => traderBook = value;
        }

        public ChartType ChartType
        {
            get => chartType;
        }

        public ChartControl(ChartType chartType,TraderBook traderBook)
        {
            InitializeComponent();

            this.traderBook = traderBook;
            if (chartType == ChartType.Week)
                traderBook.WeekChart = this;
            if (chartType == ChartType.Day)
                traderBook.DayChart = this;

            this.chartType = chartType;

            chart = chartType switch { ChartType.Day => new DayChart(traderBook,this), ChartType.Week => new WeekChart(traderBook, this), _ => new DirectXChart(traderBook,this) };
            
            // this.UpdateLayout();

            Canvas.Tag = this;
            if (chartType == ChartType.Day)
            {
                Grid.ColumnDefinitions[2].Width = new GridLength(0);
                webView.Visibility = Visibility.Collapsed;

                LineButton.Click += LineButton_Click;
                QuadraticButton.Click += QuadraticButton_Click;
                WeekButton.Click += WeekButton_Click;
            }
            else
            {
                // Grid.ColumnDefinitions[1].Width = new GridLength(0);
                Grid.ColumnDefinitions[2].Width = new GridLength(0);
                webView.Visibility = Visibility.Collapsed;
                HideButtons();
            }

            CandleWidth = 5;
            ZoomInButton.Click += ZoomInButton_Click;
            ZoomOutButton.Click += ZoomOutButton_Click;

            ScrollBar.Value = 0;
            ScrollBar.ValueChanged += ScrollBar_ValueChanged;

            BuildParameters();

            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

        }

        private void HideButtons()
        {
            QuadraticButton.Visibility = Visibility.Collapsed;
            LineButton.Visibility = Visibility.Collapsed;
            PropertiesButton.Visibility = Visibility.Collapsed;
            ScenarioButton.Visibility = Visibility.Collapsed;
            TagButton.Visibility = Visibility.Collapsed;
            DownloadButton.Visibility = Visibility.Collapsed;
            AnnotateButton.Visibility = Visibility.Collapsed;
            SquareButton.Visibility = Visibility.Collapsed;
            EditButton.Visibility = Visibility.Collapsed;
            MeasureButton.Visibility = Visibility.Collapsed;
            SaveButton.Visibility = Visibility.Collapsed;
            CopyButton.Visibility = Visibility.Collapsed;
            WeekButton.Visibility = Visibility.Collapsed;
        }

        private void WebView_CoreWebView2InitializationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            isWebViewReady = true;
            UpdateWebPage();
        }

        public void BuildParameters()
        {
            ParameterBar.Children.Clear();

            var script = chartType == ChartType.Week ? traderBook.WeekScript : traderBook.TraderScript;

            if (script != null)
            {
                foreach (var parameter in script.StrategyParameters)
                {
                    var control = new Controls.ParameterControl(parameter);
                    control.TraderBook = traderBook;
                    ParameterBar.Children.Add(control);
                    control.ValueChanged += chart.UpdateLayout;
                }
            }
        }



        public void UpdateScrollbar()
        {
            if (traderBook.mDataFile != null)
            {
                ScrollBar.Minimum = 0;
                ScrollBar.Maximum = (chartType == ChartType.Week ? traderBook.weekBars.Count : traderBook.bars.Count) - chart.BarsInPage;
                ScrollBar.Value = ScrollBar.Maximum; // (parent.bars.Count - 1) > hScrollbar.LargeChange ? (parent.bars.Count - hScrollbar.LargeChange) :
                ScrollBar.LargeChange = chart.BarsInPage / 2;
            }
        }

        private void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            chart.UpdateLayout();
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            CandleWidth++;
            if (CandleWidth > 20)
                CandleWidth = 20;
            if (chartType == ChartType.Week)
                traderBook.RunWeekScript();
            if (chartType == ChartType.Day)                         
                traderBook.RunTraderScript();
            UpdateScrollbar();
            chart.UpdateLayout();
        }

        public void UpdateChartLayout()
        {
            chart.UpdateLayout();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            CandleWidth--;
            if (CandleWidth < 1)
                CandleWidth = 1;
            if (chartType == ChartType.Week)
                traderBook.RunWeekScript();
            if (chartType == ChartType.Day)
                traderBook.RunTraderScript();
            UpdateScrollbar();
            chart.UpdateLayout();
        }

        public void UpdatePrice()
        {
            Price.Text = "";
        }

        public async void UpdateWebPage()
        {
            
            if (traderBook.DataFile != null)
            {
                await webView.EnsureCoreWebView2Async();
                var source = @"https://www.nzx.com/companies/" + traderBook.DataFile.Name + @"/announcements";
                webView.CoreWebView2.Navigate(source);
            }
        }
        public void UpdatePrice(double price)
        {

            if (price < 100)
                Price.Text = price.ToString("0.000");
            else if (price < 1000)
                Price.Text = price.ToString("0.00");
            else if (price < 100000)
                Price.Text = (price / 1000).ToString("0.00") + "K";
            else if (price < 1000000)
                Price.Text = (price / 1000).ToString("0.0") + "K";
            else
                Price.Text = (price / 1000000).ToString("0.00") + "M";
        }

        public void UpdatePropertiesBar()
        {
            if (traderBook.DataFile != null)
            {
                var bitmapImage = new BitmapImage();
                if (traderBook.DataFile.Image != null && traderBook.DataFile.Image.Length != 0)
                {
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream(traderBook.DataFile.Image))
                    {
                        ms.Seek(0, System.IO.SeekOrigin.Begin);

                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = ms;
                        bitmapImage.EndInit();

                        Image.Source = bitmapImage; // Put it in the bar
                    }
                }
                else
                {
                    Image.Source = null;
                }
                YahooCode.Content = traderBook.DataFile.YahooCode;
                Description.Content = traderBook.DataFile.Description;

                var dividends = Data.Dividend.GetYahooCode(traderBook.DataFile.YahooCode);
                dividends.Sort((x, y) => x.ExDividend.CompareTo(y.ExDividend));
                NextDividend.Content = "";
                foreach (var dividend in dividends)
                {
                    if( dividend.ExDividend  >= DateTime.Now )
                    {
                        NextDividend.Content = dividend.ExDividend.ToString("dd MMM yyy");
                        break;
                    }
                }
            }
        }

        public void UpdateDetails()
        {
            Date.Text = "";
            Open.Text = "";
            High.Text = "";
            Low.Text = "";
            Close.Text = "";
        }

        public void UpdateDetails(DateTime date, double open, double high, double low, double close, double volume)
        {
            /*
            double move;
            if (bar > 1)
            {
                move = (100.0 * (bars.Close[bar] - bars.Close[bar - 1]) / bars.Close[bar - 1]).ToString("0.00") + "%";
            }
            else
                move = bars.Volume[bar].ToString();

            date = traderBook.DataFile[i];
            */

            Date.Text = date.ToShortDateString();
            Open.Text = open.ToString("0.000");
            High.Text = high.ToString("0.000");
            Low.Text = low.ToString("0.000");
            Close.Text = close.ToString("0.000");
            Volume.Text = volume.ToString("0.000");
        }

        public void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            
        }

        public ChartControl()
        {
            InitializeComponent();
        }

        private void LineButton_Click(object sender, RoutedEventArgs e)
        {
            if( chart is DayChart dayChart)
            {
                dayChart.LineButton_Click(sender, e);
            }

        }

        private void QuadraticButton_Click(object sender, RoutedEventArgs e)
        {
            if (chart is DayChart dayChart)
            {
                dayChart.QuadraticButton_Click(sender, e);
            }
        }

        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            if (chart is DayChart dayChart)
            {
                dayChart.TextButton_Click(sender, e);
            }
        }

        private void MeasureButton_Click(object sender, RoutedEventArgs e)
        {
            if (chart is DayChart dayChart)
            {
                dayChart.MeasureButton_Click(sender, e);
            }
        }

        private void WeekButton_Click(object sender, RoutedEventArgs e)
        {

            weekControl = new ChartControl(ChartType.Week, traderBook);
            weekWindow = new WeekWindow(weekControl);
  
            weekWindow?.Show();
            weekControl.chart.UpdateLayout();
        }

        private void PropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (chart is DayChart dayChart)
            {
                if (traderBook.mDataFile != null)
                {
                    var mainWindow = Application.Current.MainWindow;
                    var pw = new Windows.DataFileWindow(traderBook.mDataFile);
                    pw.Left = mainWindow.Left + (mainWindow.ActualWidth - pw.Width) / 2;
                    pw.Top = mainWindow.Top + 78;
                    pw.ShowDialog();
                }
            }
        }

        private void Image_Drop(object sender, DragEventArgs e)
        {
            var formats = e.Data.GetFormats();

            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths  )
            {
                if (paths.Length > 0)
                {
                    var image = System.Drawing.Image.FromFile(paths[0]);
                    using (var ms = new System.IO.MemoryStream())
                    {
                        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Seek(0, System.IO.SeekOrigin.Begin);

                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = ms;
                        bitmapImage.EndInit();

                        Image.Source = bitmapImage; // Put it in the bar

                        traderBook.DataFile.Image = ms.ToArray();
                        traderBook.DataFile.Save();
                    }                   
                }
            }
            else 
            {
                // Assume it's from the web
                var source =  e.Data.GetData(typeof(string)) as string;
                using (var webClient = new System.Net.WebClient())
                {
                    webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " +
                                  "Windows NT 5.2; .NET CLR 1.0.3705;)");

                    if (source != null && source.Length > 4 && source.Substring(source.Length - 4).ToLower() == ".png")
                    {
                        // Assume it's a local PNG file
                        var bytes = webClient.DownloadData(source);

                        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes))
                        {
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = ms;
                            bitmapImage.EndInit();

                            Image.Source = bitmapImage; // Put it in the bar
                            traderBook.DataFile.Image = bytes;
                            traderBook.DataFile.Save();
                        }
                    }
                    else
                    {
                        // Assume it's an HTML file
                        HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                        htmlDoc.OptionOutputAsXml = true;

                        try
                        {
                            using (var reader = webClient.OpenRead(source))
                            {
                                htmlDoc.Load(reader);
                            }
                        }
                        catch
                        {
                            return;
                        }

                        var imageNodes = htmlDoc.DocumentNode.SelectNodes("//img");
                        if (imageNodes.Count < 2)
                            return;

                        var imageNode = imageNodes[1];

                        var src = imageNode.Attributes["src"];

                        // Need to set the header every time
                        webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " +
                            "Windows NT 5.2; .NET CLR 1.0.3705;)");

                        byte[] bytes;

                        if (src.Value.Substring(src.ValueLength - 4).ToLower() == ".svg")
                        {
                            // Handle the special case of an SVG file
                            var svgString = webClient.DownloadString(src.Value);
                            var svgDocument = Svg.SvgDocument.FromSvg<Svg.SvgDocument>(svgString);
                            var bitmap = svgDocument.Draw();

                            // Convert the bitmap to bytes
                            using (var sms = new System.IO.MemoryStream())
                            {
                                // Convert it to an SVG
                                bitmap.Save(sms, System.Drawing.Imaging.ImageFormat.Png);
                                bytes = sms.ToArray();
                            }
                        }
                        else
                        {
                            // Otherwise just go and get it from the web
                            bytes = webClient.DownloadData(src.Value);
                        }

                        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes))
                        {
                            // load the bitmapImage
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = ms;
                            bitmapImage.EndInit();

                            // Put it in the bar
                            Image.Source = bitmapImage; 

                            // Save it to the DataFile
                            traderBook.DataFile.Image = bytes;
                            traderBook.DataFile.Save();
                        }

                    }
                }
  
            }
        }
    }
}
