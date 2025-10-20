using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;
using System.Windows.Media;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader
{
    public class DirectXChart : WPFChart
    {
        Controls.ChartControl chartControl;
        DrawingVisual drawingVisual;
        VisualHost visualHost;
        protected DrawingContext context;

        static Brush borderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(a: 255, r: 38, g: 38, b: 191));
        protected static Brush transparentBrush = new SolidColorBrush(System.Windows.Media.Colors.Transparent);
        protected static Brush semiTransparentWhiteBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(a: 63, r: 255, g: 255, b: 255));
        protected static Brush blackBrush = new SolidColorBrush(System.Windows.Media.Colors.Black);
        protected static Pen blackPen = new Pen(Brushes.Black, 1);
        static protected Pen lightGrayPen = new Pen(Brushes.LightGray, 1);
        static Pen volumePen = new Pen(new SolidColorBrush(Colors.LightGray), 0.25);

        public DirectXChart(TraderBook traderBook) : base(traderBook)
        {
        }

        public DirectXChart(TraderBook traderBook, Controls.ChartControl chartControl) : base(traderBook,chartControl.Canvas)
        {
            chartControl.Tag = this;
            this.chartControl = chartControl;
            drawingVisual = new DrawingVisual();
            visualHost = new VisualHost(drawingVisual);
            visualHost.Width = canvas.ActualWidth;
            visualHost.Height = canvas.ActualHeight;

            visualHost.Visibility = Visibility.Visible;

            visualHost.MouseMove += VisualHost_MouseMove;
        }

        #region Properties

        public double YToValue(Pane pane, double y)
        {
            double val;
            double y1 = actualHeight - y;
            double range = pane.max - pane.min;
            return (y1 - pane.origin - 0.5) * range / (pane.size * (actualHeight - bottomBarHeight) / totalSize - 1.0) + pane.min;
        }

        public double YToPrice(double y)
        {
            return YToValue(traderBook.TraderScript.PricePane, y);
        }

        public double XToBar(double x)
        {
            return x / (candleWidth * 2) + FirstBar;
        }
        override protected TraderScript TraderScript
        {
            get => chartControl.ChartType == ChartType.Week ? traderBook.WeekScript : traderBook.TraderScript;
        }

        override protected Bars Bars
        {
            get => chartControl.ChartType == ChartType.Week ? traderBook.mDataFile.weekBars : traderBook.mDataFile.bars;
        }

        override public double BarsInPage
        {
            get
            {
                if (double.IsNaN(canvas.ActualWidth))
                {
                    return 1;
                }
                else
                {
                    double bars = canvas.ActualWidth - rightBorderWidth;
                    bars = Math.Floor(bars / (chartControl.CandleWidth * 2));
                    return bars <= 0 ? 1 : bars;
                }

            }
        }

        #endregion Properties

        private void VisualHost_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var point = e.MouseDevice.GetPosition(visualHost);
            chartControl?.Canvas_MouseMove(sender,e);
        }

        public class VisualHost : FrameworkElement
        {
            DrawingVisual path;
            public VisualHost(DrawingVisual path)
            {
                this.path = path;
                AddVisualChild(path);
            }

            protected override int VisualChildrenCount
            {
                get { return 1; }
            }

            protected override Visual GetVisualChild(int index)
            {
                return path;
            }
        }

        override public void UpdateLayout()
        {
            canvas.Children.Clear();


            canvas.UpdateLayout();
            context = drawingVisual.RenderOpen();

            base.UpdateLayout();

            context.Close();
            canvas.Children.Insert(0,visualHost);
        }

        override protected void WhiteBoard()
        {
            canvas.Background = Brushes.White;
        }

        override protected void DrawSideBar()
        {
            Rect rect = new Rect(actualWidth - rightBorderWidth, 0, rightBorderWidth, actualHeight);
            context.DrawRectangle(borderBrush, null, rect);
        }

        override protected void DrawBottomBar()
        {
            Rect rect = new Rect(0, actualHeight-bottomBarHeight, actualWidth, bottomBarHeight);
            context.DrawRectangle(borderBrush, null, rect);
        }

        override protected void DrawBarBackgrounds()
        {
            for (int bar = FirstBar; bar <= LastBar; bar++)
            {
                Annotation annotation = TraderScript.Annotations[bar];
                if (annotation != null)
                {
                    var color = annotation.backgroundcolor;
                    var brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
                    var pen = new Pen(brush, candleWidth * 2);
                    var x = (float)BarToX(bar);
                    context.DrawLine(pen, new Point(x, 0), new Point(x, canvas.ActualHeight - bottomBarHeight));
                }
            }
        }

        override protected void DrawTrades()
        {
            for (int bar = FirstBar; bar <= LastBar; bar++)
            {
                DateTime barDate = Bars.Date[bar];
                foreach (Data.Trade trade in traderBook.Trades)
                {
                    DateTime tradeDate = trade.Date;
                    if (barDate.Date.Date == trade.Date.Date)
                    {
                        double x = BarToX(bar);

                        var brush = new SolidColorBrush(
                        trade.Quantity > 0
                        ? Color.FromArgb(255, 50, 205, 50)
                        : Color.FromArgb(255, 255, 0, 0)
                        );

                        var pen = new Pen(brush,1);
                        context.DrawEllipse(brush, pen, new Point(x, actualHeight - TraderScript.PricePane.origin), candleWidth, candleWidth);
                    }
                }
            }
        }

        protected override void LoadBackground()
        {
        }

        override protected void DrawScriptAnnotations()
        {
            // Draw bar backgrounds.  We want everything else to draw over the top of these
            // that's why they're first

            for (int bar = FirstBar; bar <= LastBar; bar++)
            {
                DateTime barDate = TraderScript.bars.Date[bar];
                foreach (Data.Annotation annotation in traderBook.Annotations)
                {
                    DateTime annotationDate = annotation.Date;
                    if (barDate.Date == annotation.Date)
                    {

#pragma warning disable CS0618 // Type or member is obsolete
                        var formattedText = new FormattedText(annotation.Text,
                            System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name),
                            12,
                            Brushes.Black);
#pragma warning restore CS0618 // Type or member is obsolete

                        Point point = new Point(BarToX(bar) - formattedText.Width / 2f, ValueToY(traderBook.TraderScript.PricePane, annotation.Price) - formattedText.Height * 1.2f);

                        context.DrawText(formattedText,point);
                    }
                }
            }
        }


        override protected void DrawAnnotations()
        {
            for (int bar = FirstBar; bar <= LastBar; bar++)
            {
                Annotation annotation = TraderScript.Annotations[bar];
                foreach (Position position in annotation.position)
                {
                    if (position.OpenBar == bar)
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                          var extents = new FormattedText(position.OpenSignal,
                              System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                              FlowDirection.LeftToRight,
                              new Typeface(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name),
                              12, 
                              Brushes.Black);
#pragma warning restore CS0618 // Type or member is obsolete

                        Point point = new Point(BarToX(bar) - extents.Width / 2f, annotation.bottom - extents.Height * 1.2f);

                        context.DrawText(extents,point);
                        annotation.bottom -= extents.Height * 1.2;
                    }
                    if (position.CloseBar == bar)
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        var extents = new FormattedText(position.CloseSignal,
                            System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name),
                            12,
                            Brushes.Black);
#pragma warning restore CS0618 // Type or member is obsolete

                        Point point = new Point(BarToX(bar) - extents.Width / 2f, annotation.bottom - extents.Height * 1.2f);
                        context.DrawText(extents,point);
                        annotation.top += extents.Height * 1.2;
                    }
                }


                double diff = 0;
                foreach (ColorString colorstring in annotation.colorstring)
                {
                    FormattedText extents;

                    if (colorstring.Font != null)
                    {
                        Brush brush = new SolidColorBrush(Color.FromArgb(colorstring.Color.A,colorstring.Color.R, colorstring.Color.G, colorstring.Color.B));
#pragma warning disable CS0618 // Type or member is obsolete
                        extents = new FormattedText(colorstring.Text,
                            System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface(colorstring.Font.FontFamily.Name),
                            colorstring.Font.Size,
                            brush);
#pragma warning restore CS0618 // Type or member is obsolete

                        extents.SetFontWeight(colorstring.Font.Bold ? FontWeights.Bold : FontWeights.Normal);
                        extents.SetFontStyle(colorstring.Font.Bold ? FontStyles.Italic : FontStyles.Normal);
                    }
                    else
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        extents = new FormattedText(colorstring.Text,
                            System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name),
                            12,
                            Brushes.Black);
#pragma warning restore CS0618 // Type or member is obsolete
                    }

                    // context.MoveTo((float)BarToX(bar) - extents.Size.Width / 2.0f, (float) annotation.top - extents.Size.Height * 0.2f);
                    // context.ShowText(colorstring.Text);
                    Point point = new Point(
                        BarToX(bar) - extents.Width / 2.0f,
                        annotation.top + extents.Height * 0.2f + diff
                    ); ;
                    context.DrawText(extents,point);
                    diff += extents.Height;
                }

            }
        }

        override protected void executePlotPattern(Pane pane, double width, double height, object[] parameters)
        {

            var bars = (int[])parameters[0];

            int firstBar = FirstBar;
            int lastBar = LastBar;
            bool found = false;
            for (int i=0; i < bars.Length; i++)
            {
                if (bars[i] >= firstBar && bars[i] <= lastBar)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                return;

            var thickness = (int)parameters[3];
            var lineStyle = (LineStyle)parameters[2];
            var color = MediaColor((System.Drawing.Color)parameters[1]);

            int paneSize = pane.size;
            int paneOrigin = pane.origin;
            double paneMin = pane.min;
            double paneMax = pane.max;
            double actualHeight = canvas.ActualHeight;

            double ValueToY(double val)
            {
                return this.ValueToY(paneSize, paneOrigin, paneMin, paneMax, actualHeight, val);
            }

            try
            {
                var brush = new SolidColorBrush(color);
                var pen = new Pen(brush,thickness);
                pen.DashStyle = lineStyle switch
                {
                    LineStyle.Dashes => DashStyles.Dash,
                    LineStyle.Dots => DashStyles.Dot,
                    _ => DashStyles.Solid
                };

                for (int i = 1; i < bars.Length; i++)
                {
                    double x1 = BarToX(bars[i - 1]);
                    double y1 = ValueToY(Bars.Close[bars[i - 1]]);
                    double x2 = BarToX(bars[i]);
                    double y2 = ValueToY(Bars.Close[bars[i]]);

                    context.DrawLine(pen, new Point(x1,y1), new Point(x2,y2));
                }
            }
            catch (Exception debugException)
            {
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(debugException, true);
                string stringLineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
                string message = "(" + stringLineNumber + ") " + debugException.Message;
                MessageBox.Show(message);
            }
        }


        override protected void PlotPositions()
        {
            var lastBar = LastBar;

            for (int bar = FirstBar; bar <= lastBar; bar++)
            {
                Annotation annotation = TraderScript.Annotations[bar];
                annotation.top = ValueToY(TraderScript.PricePane, traderBook.bars.Low[bar]);
                annotation.bottom = ValueToY(TraderScript.PricePane, traderBook.bars.High[bar]);
                foreach (Position position in annotation.position)
                {
                    if (position.OpenBar == bar)
                    {
                        double topy = annotation.bottom;
                        double bottomy = topy - candleWidth * Math.Sqrt(3.0);
                        double topx = BarToX(position.OpenBar) + candleWidth / 2;
                        double leftx = topx - candleWidth / 2.0;
                        double rightx = leftx + candleWidth;

                        var brush = new SolidColorBrush(Color.FromArgb(255, 166, 166, 166));

                        StreamGeometry geometry = new StreamGeometry();
                        using (StreamGeometryContext gc = geometry.Open())
                        {
                            // Start new object, filled=true, closed=true
                            gc.BeginFigure(new Point(topx, topy), true, true);

                            gc.LineTo(new Point(rightx, bottomy), true, true);
                            gc.LineTo(new Point(leftx, bottomy), true, true);
                            gc.LineTo(new Point(topx, topy), true, true);
                        }

                        context.DrawGeometry(brush, new Pen(Brushes.Black, 1), geometry);


                        annotation.bottom = bottomy;
                    }

                    if (position.CloseBar == bar)
                    {
                        double topy = annotation.top;
                        double bottomy = topy + candleWidth * Math.Sqrt(3.0);
                        double topx = BarToX(position.CloseBar) + candleWidth / 2;
                        double leftx = topx - candleWidth / 2.0;
                        double rightx = leftx + candleWidth;

                        StreamGeometry geometry = new StreamGeometry();
                        using (StreamGeometryContext gc = geometry.Open())
                        {
                            // Start new object, filled=true, closed=true
                            gc.BeginFigure(new Point(topx, topy), true, true);

                            gc.LineTo(new Point(rightx, bottomy), true, true);
                            gc.LineTo(new Point(leftx, bottomy), true, true);
                            gc.LineTo(new Point(topx, topy), true, true);
                        }

                        var fillBrush = new SolidColorBrush(position.ClosePrice < position.OpenPrice ? Colors.Red : position.ClosePrice > position.OpenPrice ? Colors.Green : Color.FromArgb(255, 166, 166, 166));
                        var lineBrush = new SolidColorBrush(
                            position.ClosePrice < position.OpenPrice ? Color.FromArgb(255, 77, 0, 0)
                            : position.ClosePrice > position.OpenPrice ? Color.FromArgb(255, 0, 77, 0)
                            : Color.FromArgb(255, 166, 166, 166)
                        );
                        context.DrawGeometry(fillBrush, new Pen(lineBrush, 1), geometry);

                        annotation.top = bottomy;
                    }
                }
            }

        }

        override protected void ShowVolumePriceValues()
        {
            // Cairo.Context context = Gdk.CairoHelper.Create(drawingarea.Window);
            // For each pane draw lines to show values
            try
            {
                foreach (Pane pane in new List<Pane>() { TraderScript.VolumePane, TraderScript.PricePane })
                {
                    Size textextents = TextExtents("0.123456789", FontWeights.Normal, 12.0f);
                    double range = pane.max - pane.min;
                    double maxnovalues = pane.size * (actualHeight-bottomBarHeight) / totalSize / (textextents.Height * 3);
                    double maxincrement = range / maxnovalues;
                    double logscale = Math.Log(maxincrement, 10);
                    double exponent = Math.Floor(logscale);
                    double fraction = Math.Pow(10, logscale - exponent);
                    double scale = 1;
                    foreach (double testscale in new double[] { 2, 2.5, 5, 10 })
                    {

                        if (testscale > fraction)
                        {
                            scale = testscale;
                            break;
                        }
                    }
                    scale = exponent + Math.Log(scale, 10);
                    scale = Math.Pow(10, scale);


                    double start = Math.Ceiling(pane.min / scale) * scale;
                    double end = Math.Floor(pane.max / scale) * scale;

                    for (double val = start; val <= end; val += scale)
                    {
                        double y = ValueToY(pane, val);

                        string valstring;
                        if (Math.Abs(val) < 1000)
                            valstring = string.Format("{0:#.##}", val);
                        else if (Math.Abs(val) < 100000)
                            valstring = val.ToString();
                        else
                            valstring = string.Format("{0:0.0000e0}", val);

                        context.DrawLine(volumePen, new Point(0, y), new Point(actualWidth - rightBorderWidth, y));

#pragma warning disable CS0618 // Type or member is obsolete
                        var formattedText = new FormattedText(valstring,
                            System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name),
                            12,
                            Brushes.White);
#pragma warning restore CS0618 // Type or member is obsolete

                        // actualWidth - rightBorderWidth
                        context.DrawText(formattedText, new Point(actualWidth-rightBorderWidth,y-formattedText.Height/2));   
                    }
                }
            }
            catch (Exception debugException)
            {
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(debugException, true);
                string stringLineNumber = stack.GetFrame(1).GetFileLineNumber().ToString();
                string message = "(" + stringLineNumber + ") " + debugException.Message;
                MessageBox.Show(message);
            }
        }

        override protected void DrawMonthLines()
        {
            // Cairo.Context context = Gdk.CairoHelper.Create(drawingarea.GdkWindow);
            // Draw month bars and months
            int currentmonth = 0;
            var bars = TraderScript.bars;

            try
            {
                for (int i = (int)FirstBar; i <= LastBar; i++)
                {
                    DateTime date = bars.Date[i];

                    if (date.Month != currentmonth && date.Day <= 7)
                    {
                        currentmonth = date.Month;

                        double x = BarToX(i);
                        context.DrawLine(lightGrayPen, new Point(x,0), new Point(x,actualHeight));


                        string datestring = date.Day + "/" + date.Month + "/" + date.Year;

#pragma warning disable CS0618 // Type or member is obsolete
                        var formattedText = new FormattedText(datestring,
                            System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name),
                            chartControl.CandleWidth < 5 ? 7 + chartControl.CandleWidth : 11,
                            Brushes.White);
#pragma warning restore CS0618 // Type or member is obsolete

                        // actualWidth - rightBorderWidth
                        Point point = new Point(x - formattedText.Width / 2, actualHeight - bottomBarHeight / 2 - formattedText.Height / 2);
                        context.DrawText(formattedText, point);
                    }
                }
            }
            catch (Exception debugException)
            {
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(debugException, true);
                string stringLineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
                string message = "(" + stringLineNumber + ") " + debugException.Message;
                MessageBox.Show(message);
            }
        }

        override protected void DrawLabels()
        {
            foreach (Pane pane in TraderScript.Panes)
            {
                double y = ValueToY(pane, pane.max) + 5;
                foreach (ColorString label in pane.PaneLabels)
                {

#pragma warning disable CS0618 // FormattedText is obsolete, but it's needed for DrawText
                    var formattedText = new FormattedText(label.Text,
                        System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name),
                        12,
                        new SolidColorBrush(MediaColor(label.Color)));
#pragma warning restore CS0618 // FormattedText is obsolete, but it's needed for DrawText


                    context.DrawText(formattedText, new Point(5, y));

                    y += formattedText.Height + 5;
                }
            }
        }


        override protected void ShowLastValues()
        {
            // Cairo.Context context = Gdk.CairoHelper.Create(drawingarea.GdkWindow);
            // Show the values of the most recent items
            foreach (Pane pane in TraderScript.Panes)
            {
                if (pane == TraderScript.PricePane)
                    continue;

                foreach (DataSeries ds in pane.DataSeriess)
                {
                    double val = ds[LastBar];
                    string valstring;
                    if (Math.Abs(val) < 1000)
                        valstring = string.Format("{0:#.##}", val);
                    else if (Math.Abs(val) < 100000)
                        valstring = val.ToString();
                    else
                        valstring = string.Format("{0:0.0000e0}", val);

                    double y = ValueToY(pane, val);

#pragma warning disable CS0618 // Type or member is obsolete
                    var extents = new FormattedText(valstring,
                        System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name),
                        12,
                        Brushes.White);
#pragma warning restore CS0618 // Type or member is obsolete

                    // actualWidth - rightBorderWidth
                    Point point = new Point(actualWidth - rightBorderWidth, y - extents.Height / 2);
                    context.DrawText(extents, point);
                }

            }
        }

        override protected void DrawLine(int width, int height)
        {
            context.DrawLine(blackPen, new Point(0, actualHeight - height), new Point(width, canvas.ActualHeight - height));
        }

        #region Execute methods from TraderScript
        override protected void executePlotPrices(Pane pane, double width, double height, object[] parameters)
        {

            Bars bars = (Bars)parameters[0];
            int count = bars.Count;
            int paneSize = pane.size;
            int paneOrigin = pane.origin;
            double paneMin = pane.min;
            double paneMax = pane.max;
            double actualHeight = canvas.ActualHeight;

            double ValueToY(double val)
            {
                return this.ValueToY(paneSize, paneOrigin, paneMin, paneMax, actualHeight, val);
            }

            try
            {
                int firstBar = FirstBar;
                int lastBar = LastBar ;

                for (int i=firstBar; i<=lastBar; i++)
                {
                    double x = BarToX(firstBar, i);
                    Brush color;
                    double open = bars.Open[i];
                    double close = bars.Close[i];
                    double low = bars.Low[i];
                    double high = bars.High[i];

                    if (open > close)
                        color = Brushes.Red;
                    else if (open == close)
                        color = Brushes.Black;
                    else
                        color = Brushes.Green;


                    var wick = new Pen(color, 1);
                    var body = new Pen(color, open != close ? candleWidth : 1);

                    context.DrawLine(wick, new Point(x, ValueToY(Math.Max(open, close))), new Point(x, ValueToY(high)));
                    context.DrawLine(wick, new Point(x, ValueToY(Math.Min(open, close))), new Point(x, ValueToY(low)));
                    if (open == close)
                        context.DrawLine(body, new Point(x-candleWidth/2, ValueToY(Math.Min(open, close))), new Point(x + candleWidth, ValueToY(Math.Max(open, close))));
                    else
                        context.DrawLine(body, new Point(x, ValueToY(Math.Min(open, close))), new Point(x, ValueToY(Math.Max(open, close))));
                }


            }
            catch (Exception debugException)
            {
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(debugException, true);
                string stringLineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
                string message = "(" + stringLineNumber + ") " + debugException.Message;
                MessageBox.Show(message);
            }
        }

        override protected void executePlotHistogram(Pane pane, double width, double height, object[] parameters)
        {
            int paneSize = pane.size;
            int paneOrigin = pane.origin;
            double paneMin = pane.min;
            double paneMax = pane.max;
            double actualHeight = canvas.ActualHeight;
            int firstBar = FirstBar;
            int lastBar = LastBar;

            double ValueToY(double val)
            {
                return this.ValueToY(paneSize, paneOrigin, paneMin, paneMax, actualHeight, val);
            }

            double BarToX(int i)
            {
                return this.BarToX(firstBar, i);
            }

            DataSeries ds = (DataSeries)parameters[0];
            Color color = (System.Windows.Media.Color)parameters[1];
            int thickness = (int)parameters[2];
            if (thickness <= 0)
                thickness = (int) candleWidth;

            double max = pane.max;
            double min = pane.min;

            double range = max - min;
            if (range == 0) range = 1;

            try
            {
                for(int i=firstBar; i<=lastBar; i++)
                {

                    var brush = new SolidColorBrush(color);

                    var pen = new Pen(brush, thickness);

                    context.DrawLine(pen,
                        new Point(BarToX(i), ValueToY(ds[i])),
                        new Point(BarToX(i), actualHeight - paneOrigin)
                        );
                }
            }
            catch (Exception debugException)
            {
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(debugException, true);
                string stringLineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
                string message = "(" + stringLineNumber + ") " + debugException.Message;
                MessageBox.Show(message);
            }
        }

        override protected void executePlotSeries(Pane pane, double width, double height, object[] parameters)
        {
            int paneSize = pane.size;
            int paneOrigin = pane.origin;
            double paneMin = pane.min;
            double paneMax = pane.max;
            double actualHeight = canvas.ActualHeight;
            int firstBar = FirstBar;
            int lastBar = LastBar;

            double ValueToY(double val)
            {
                return this.ValueToY(paneSize, paneOrigin, paneMin, paneMax, actualHeight, val);
            }

            double BarToX(int i)
            {
                return this.BarToX(firstBar, i);
            }

            DataSeries ds = (DataSeries)parameters[0];
            Color color;
            try
            {
                color = (Color)parameters[1];
            }
            catch
            {
                color = MediaColor((System.Drawing.Color)parameters[1]);
            }

            int thickness = (int)parameters[3];
            LineStyle linestyle = (LineStyle)parameters[2];

            if (linestyle == LineStyle.Histogram)
            {
                executePlotHistogram(pane, width, height, new object[] { (object)ds, (object)color, (object)thickness });
                return;
            }

            try
            {

                var brush = new SolidColorBrush(color);
                var pen = new Pen(brush,thickness);

                pen.DashStyle = linestyle switch
                {
                    LineStyle.Dots => DashStyles.Dot,
                    LineStyle.Dashes => DashStyles.Dash,
                    _ => DashStyles.Solid
                };

                for(int i=firstBar; i< lastBar; i++)
                {
                    double y1 = ValueToY(ds[i]);
                    double x1 = BarToX(i);
                    double x2 = BarToX(i + 1);
                    double y2 = ValueToY(ds[i + 1]);

                    context.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
                }
            }
            catch (Exception debugException)
            {
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(debugException, true);
                string stringLineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
                string stringMethod = stack.GetFrame(0).GetMethod().Name;
                string message = "(" + stringMethod + ", " + stringLineNumber + ") " + debugException.Message;
                MessageBox.Show(message);
            }
        }

        override protected void executePlotSeriesFillBand(Pane pane, double width, double height, object[] parameters)
        {
            int paneSize = pane.size;
            int paneOrigin = pane.origin;
            double paneMin = pane.min;
            double paneMax = pane.max;
            double actualHeight = canvas.ActualHeight;
            int firstBar = FirstBar;
            int lastBar = LastBar;

            double ValueToY(double val)
            {
                return this.ValueToY(paneSize, paneOrigin, paneMin, paneMax, actualHeight, val);
            }

            double BarToX(int i)
            {
                return this.BarToX(firstBar, i);
            }

            try
            {
                DataSeries ds1 = (DataSeries)parameters[0];
                DataSeries ds2 = (DataSeries)parameters[1];
                Color linecolor = MediaColor((System.Drawing.Color)parameters[2]);
                Color bandcolor = MediaColor((System.Drawing.Color)parameters[3]);
                LineStyle linestyle = (LineStyle)parameters[4];
                int thickness = (int)parameters[5];



                // context.Translate(0, 0); // Move the origin to the bottom

                double max = pane.max;
                double min = pane.min;
                int origin = pane.origin;


                for(int i=firstBar; i<lastBar; i++)
                {
                    double upper_y = ValueToY(ds1[i]);
                    double x = BarToX(i);


                    double lower_y = ValueToY(ds2[i]);


                    double lower_y1 = ValueToY(ds2[i + 1]);
                    double x1 = BarToX(i + 1);


                    double upper_y1 = ValueToY(ds1[i + 1]);



                    var brush = new SolidColorBrush(bandcolor);

                    StreamGeometry geometry = new StreamGeometry();
                    using (StreamGeometryContext gc = geometry.Open())
                    {
                        // Start new object, filled=true, closed=true
                        gc.BeginFigure(new Point(x, upper_y), true, true);

                        gc.LineTo(new Point(x, lower_y), true, true);
                        gc.LineTo(new Point(x1, lower_y1), true, true);
                        gc.LineTo(new Point(x1, lower_y1), true, true);
                        gc.LineTo(new Point(x1, upper_y1), true, true);
                        gc.LineTo(new Point(x, upper_y), true, true);
                    }

                    context.DrawGeometry(brush, null, geometry);              
                }

                if (linecolor != Colors.Transparent)
                {
                    object[] series1parameters = new object[] { (object)ds1, (object)linecolor, (object)linestyle, (object)thickness };
                    object[] series2parameters = new object[] { (object)ds2, (object)linecolor, (object)linestyle, (object)thickness };
                    executePlotSeries(pane, width, height, series1parameters);
                    executePlotSeries(pane, width, height, series2parameters);
                }
            }
            #region debug
            catch (Exception debugException)
            {
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(debugException, true);
                string stringLineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
                string message = "(" + stringLineNumber + ") " + debugException.Message;
                MessageBox.Show(message);
            }
            #endregion
        }

        override protected void executePlotSeriesOscillator(Pane pane, double width, double height, object[] parameters)
        {
            int paneSize = pane.size;
            int paneOrigin = pane.origin;
            double paneMin = pane.min;
            double paneMax = pane.max;
            double actualHeight = canvas.ActualHeight;

            double ValueToY(double val)
            {
                return this.ValueToY(paneSize, paneOrigin, paneMin, paneMax, actualHeight, val);
            }

            try
            {
                // DataSeries source, double overbought, double oversold, System.Drawing.Color overboughtColor, System.Drawing.Color oversoldColor, System.Drawing.Color color, LineStyle style, int width
                DataSeries ds1 = (DataSeries)parameters[0];
                double overbought = (double)parameters[1];
                double oversold = (double)parameters[2];
                Color overboughtColor = MediaColor((System.Drawing.Color)parameters[3]);
                Color oversoldColor = MediaColor((System.Drawing.Color)parameters[4]);
                Color linecolor = MediaColor((System.Drawing.Color)parameters[5]);
                LineStyle linestyle = (LineStyle)parameters[6];
                int thickness = (int)parameters[7];

                if (linecolor != System.Windows.Media.Colors.Transparent)
                {
                    object[] series1parameters = new object[] { (object)ds1, (object)linecolor, (object)linestyle, (object)thickness };
                    executePlotSeries(pane, width, height, series1parameters);
                }

                // context.Translate(0, 0); // Move the origin to the bottom


                // context.LineWidth = thickness;

                int firstBar = FirstBar;
                int lastBar = LastBar;

                ParallelOptions options = new ParallelOptions();
                options.MaxDegreeOfParallelism = processorCount;
                for(int i=firstBar; i< lastBar; i++)
                {


                    if (ds1[i] <= oversold || ds1[i + 1] <= oversold)
                    {
                        double x1 = BarToX(firstBar, i);
                        double y1 = ValueToY(ds1[i]);
                        double x2 = BarToX(firstBar, i + 1);
                        double y2 = ValueToY(ds1[i + 1]);
                        if (ds1[i] > oversold)
                        {
                            double slope = (ValueToY(ds1[i + 1]) - ValueToY(ds1[i])) / (BarToX(firstBar, i + 1) - BarToX(firstBar, i));
                            double intercept = ValueToY(ds1[i]) - slope * BarToX(firstBar, i);
                            x1 = (ValueToY(oversold) - intercept) / slope;
                            y1 = x1 * slope + intercept;
                        }
                        else if (ds1[i + 1] > oversold)
                        {
                            double slope = (ValueToY(ds1[i + 1]) - ValueToY(ds1[i])) / (BarToX(firstBar, i + 1) - BarToX(firstBar, i));
                            double intercept = ValueToY(ds1[i]) - slope * BarToX(firstBar, i);
                            x2 = (ValueToY(oversold) - intercept) / slope;
                            y2 = x2 * slope + intercept;
                        }

                        var lineBrush = new SolidColorBrush(linecolor);
                        var oversoldBrush = new SolidColorBrush(oversoldColor);
                        var linePen = new Pen(lineBrush,thickness);

                        StreamGeometry geometry = new StreamGeometry();
                        using (StreamGeometryContext gc = geometry.Open())
                        {
                            // Start new object, filled=true, closed=true
                            gc.BeginFigure(new Point(x1, y1), true, true);

                            // isStroked=true, isSmoothJoin=true
                            gc.LineTo(new Point(x1, ValueToY(oversold)), true, true);
                            gc.LineTo(new Point(x2, ValueToY(oversold)), true, true);
                            gc.LineTo(new Point(x2, y2), true, true);
                            gc.LineTo(new Point(x1, y1), true, true);
                        }

                        context.DrawGeometry(oversoldBrush, linePen, geometry);

                    }

                    if (ds1[i] >= overbought || ds1[i + 1] >= overbought)
                    {
                        double x1 = BarToX(firstBar, i);
                        double y1 = ValueToY(ds1[i]);
                        double x2 = BarToX(firstBar, i + 1);
                        double y2 = ValueToY(ds1[i + 1]);
                        if (ds1[i] < overbought)
                        {
                            double slope = (ValueToY(ds1[i + 1]) - ValueToY(ds1[i])) / (BarToX(firstBar, i + 1) - BarToX(firstBar, i));
                            double intercept = ValueToY(ds1[i]) - slope * BarToX(firstBar, i);
                            x1 = (ValueToY(overbought) - intercept) / slope;
                            y1 = x1 * slope + intercept;
                        }
                        else if (ds1[i + 1] < overbought)
                        {
                            double slope = (ValueToY(ds1[i + 1]) - ValueToY(ds1[i])) / (BarToX(firstBar, i + 1) - BarToX(firstBar, i));
                            double intercept = ValueToY(ds1[i]) - slope * BarToX(firstBar, i);
                            x2 = (ValueToY(overbought) - intercept) / slope;
                            y2 = x2 * slope + intercept;
                        }

                        var lineBrush = new SolidColorBrush(linecolor);
                        var overboughtBrush = new SolidColorBrush(overboughtColor);
                        var linePen = new Pen(lineBrush, thickness);

                        StreamGeometry geometry = new StreamGeometry();
                        using (StreamGeometryContext gc = geometry.Open())
                        {
                            // Start new object, filled=true, closed=true
                            gc.BeginFigure(new Point(x1, y1), true, true);

                            // isStroked=true, isSmoothJoin=true
                            gc.LineTo(new Point(x1, ValueToY(overbought)), true, true);
                            gc.LineTo(new Point(x2, ValueToY(overbought)), true, true);
                            gc.LineTo(new Point(x2, y2), true, true);
                            gc.LineTo(new Point(x1, y1), true, true);
                        }

                        context.DrawGeometry(overboughtBrush, linePen, geometry);
                    }
                }
            }
            #region debug
            catch (Exception debugException)
            {
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(debugException, true);
                string stringLineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
                string message = "(" + stringLineNumber + ") " + debugException.Message;
                MessageBox.Show(message);
            }
            #endregion
        }

        protected override void DrawAnnotationShapes()
        {
            var traderScript = TraderScript;
            var lastBar = LastBar;

            for (int bar = FirstBar; bar <= lastBar; bar++)
            {
                Annotation annotation = traderScript.Annotations[bar];
                foreach (var shapeDetail in annotation.shapes)
                {
                    var x = (float)BarToX(bar);
                    var y = (float)ValueToY(traderScript.PricePane, shapeDetail.Price);

                    var color = Color.FromRgb(shapeDetail.Color.R, shapeDetail.Color.G , shapeDetail.Color.B);
                    var brush = new SolidColorBrush(color);
                    var pen = new Pen(brush, 1);

                    switch (shapeDetail.Shape)
                    {
                        case Shape.Circle:
                            var radius = (float)candleWidth / 2;
                            context.DrawEllipse(brush,pen, new Point(x,y), radius, radius);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        #endregion Execute
    }
}
