using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.CSharp;
using System.IO;
using System.Windows.Shapes;
using OpenTrader.Widgets;
using System.Threading.Tasks;

namespace OpenTrader
{
    public class WPFChart : IChart
    {
        protected Canvas canvas;
        private ScrollBar hScrollbar;
        public ParameterBar mParameterBar;
        protected double actualHeight;
        protected double actualWidth;



        protected System.Drawing.Bitmap backgroundImage;

        protected const int bottomBarHeight = 20;
        protected const int rightBorderWidth = 50;
        protected int processorCount = 1;

        public int totalSize;

        protected TraderBook traderBook;

        int height, width;

        public double candleWidth=5;

        virtual protected TraderScript TraderScript
        {
            get => traderBook.TraderScript;
        }

        virtual protected Bars Bars
        {
            get
            {
                return TraderScript.TraderBook.DataFile.bars;
            }
        }

        public int FirstBar
        {
            get => (int) (canvas.Tag as Controls.ChartControl).ScrollBar.Value;
        }

        virtual public int LastBar
        {
            get { return (int)Math.Min(FirstBar + BarsInPage, TraderScript.bars.Count) - 1; }
        }

        public double BarToX(int bar)
        {
            return (bar - FirstBar) * candleWidth * 2 + candleWidth;
        }

        public double BarToX(int firstBar, int bar)
        {
            return (bar - firstBar) * candleWidth * 2 + candleWidth;
        }

        public double ValueToY(Pane pane, double val)
        {
            double range = pane.max - pane.min;
            if (range == 0) range = 1;
            double result = actualHeight - (pane.origin + 0.5 + (val - pane.min) / range * (pane.size * (actualHeight-bottomBarHeight) / totalSize - 1.0));
            return result;
        }

        public double ValueToY(int paneSize, int paneOrigin, double paneMin, double paneMax, double actualHeight, double val)
        {
            double range = paneMax - paneMin;
            if (range == 0) range = 1;
            double result = actualHeight - (paneOrigin + 0.5 + (val - paneMin) / range * (paneSize * (actualHeight - bottomBarHeight) / totalSize - 1.0));
            return result;
        }

        public WPFChart(TraderBook traderBook)
        {
            this.traderBook = traderBook;
            canvas = new Canvas();
            processorCount = Environment.ProcessorCount;
        }

        public WPFChart(TraderBook traderBook, Canvas canvas)
        {
            this.traderBook = traderBook;
            this.canvas = canvas;
            processorCount = Environment.ProcessorCount;
        }



        void MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var mouse = e.MouseDevice;
            Point p = mouse.GetPosition(canvas);
        }

        public ParameterBar ParameterBar
        {
            get { return mParameterBar; }
        }

        virtual public double BarsInPage
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
                    bars = Math.Floor(bars / (candleWidth * 2));
                    return bars <= 0 ? 1 : bars;
                }

            }
        }

        protected void CalculateTotalPaneSize()
        {
            totalSize = 0;
            if (TraderScript.Panes != null)
                foreach (Pane pane in TraderScript.Panes)
                {
                    pane.chart = this;
                    totalSize += pane.size;
                }
        }

        public static explicit operator Canvas (WPFChart wpfChart) { return wpfChart.canvas; }

        public void QueueDraw()
        {
            // UpdateScrollbar();

            UpdateLayout();
            canvas.UpdateLayout();
            // this.Show();
        }

        public void ParameterChanged(object sender, EventArgs e)
        {
            QueueDraw();
        }

        public virtual void UpdateLayout()
        {
            var dv = new DrawingVisual();

            canvas.UpdateLayout();
            actualHeight = canvas.ActualHeight;
            actualWidth = canvas.ActualWidth;
            width = (int) (canvas.ActualWidth - canvas.Margin.Left - canvas.Margin.Right);
            height = (int) (canvas.ActualHeight - canvas.Margin.Top - canvas.Margin.Bottom);

            

            WhiteBoard();
            if (TraderScript == null)
                return;
            if (TraderScript.Panes == null)
                return;

            DrawBottomBar();
            DrawSideBar();

            CalculateTotalPaneSize();
            DrawBarBackgrounds();
            DrawDividendBackgrounds();
            DrawPaneSeparators();
            DrawTrades();
            // DrawStops();
            // DrawTargets();

            CalculatePaneMaximaMinima();
            ShowLastValues();
            ShowVolumePriceValues();
            DrawMonthLines();

            ExecuteMethods();
            DrawLabels();
            PlotPositions();
            DrawAnnotations();
            DrawScriptAnnotations();
            DrawTrendLines();
            DrawAnnotationShapes();
            LoadBackground();
        }

        virtual protected void LoadBackground()
        {
            ImageBrush ib = new ImageBrush();
            ib.ImageSource = ToBitmapImage(backgroundImage);
            canvas.Background = ib;
        }

        public System.Windows.Media.Imaging.BitmapImage ToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using (var memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;

                var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }


        virtual protected void DrawSideBar()
        {
            // Draw a rectangle
            // Cairo.Context context = Gdk.CairoHelper.Create(drawingarea.GdkWindow);
            Rectangle rect = new Rectangle();
            rect.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb( a:255, r: 38, g: 38, b:191 ));
            rect.StrokeThickness = 1;
            rect.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(a: 255, r: 38, g: 38, b: 191));
            rect.Width = rightBorderWidth;
            rect.Height = actualHeight;
            Canvas.SetLeft(rect, actualWidth - rightBorderWidth);
            Canvas.SetBottom(rect, 0);
            canvas.Children.Add(rect);
            // context.SetSourceRGB( 0.75, 0.75, 1.0 );
            // context.LineWidth = 1;
            // context.MoveTo( width,0 );
            // context.LineTo( width, height );
            // context.LineTo( drawingarea_width, height );
            // context.LineTo( drawingarea_width, 0);
            // context.LineTo( width,0 );
            // context.SetSourceRGB( 0.75, 0.75, 1.0 );
            // context.FillPreserve();		
            // context.Stroke();

        }

        virtual protected void DrawTrades()
        {
            using (var graphics = System.Drawing.Graphics.FromImage(backgroundImage))
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
                            var rect = new System.Drawing.Rectangle(
                                (int) (x - candleWidth), 
                                (int) (actualHeight - TraderScript.PricePane.origin - candleWidth), 
                                (int) (candleWidth * 2), 
                                (int) (candleWidth * 2)
                            );

                            using (var brush = new System.Drawing.SolidBrush(
                                trade.Quantity > 0
                                ? System.Drawing.Color.FromArgb(50, 205, 50)
                                : System.Drawing.Color.FromArgb(255, 0, 0)
                                )
                            ) 
                            {
                                graphics.FillEllipse(brush, rect);

                                using (var pen = new System.Drawing.Pen(brush) { Width = 1 })
                                {
                                    graphics.DrawEllipse(pen, rect);
                                }
                            }
                        }
                    }
                }
            }
        }

        virtual protected void PlotPositions()
        {
            using( var graphics = System.Drawing.Graphics.FromImage(backgroundImage) )
            {
                for (int bar = FirstBar; bar <= LastBar; bar++)
                {
                    Annotation annotation = TraderScript.Annotations[bar];
                    annotation.bottom = ValueToY(TraderScript.PricePane, Bars.Low[bar]);
                    annotation.top = ValueToY(TraderScript.PricePane, Bars.High[bar]);
                    foreach (Position position in annotation.position)
                    {
                        if (position.OpenBar == bar)
                        {
                            double topy = annotation.bottom;
                            double bottomy = topy - candleWidth * Math.Sqrt(3.0);
                            double topx = BarToX(position.OpenBar);
                            double leftx = topx - candleWidth / 2.0;
                            double rightx = leftx + candleWidth;

                            System.Drawing.Point[] points =
                            {
                            new System.Drawing.Point((int) topx, (int) topy),
                            new System.Drawing.Point((int) rightx, (int) bottomy),
                            new System.Drawing.Point((int) leftx, (int) bottomy),
                            new System.Drawing.Point((int) topx, (int) topy)
                            };



                            using (var fillBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(166, 166, 166)))
                            {
                                graphics.FillPolygon(fillBrush, points);
                            }

                            using (var lineBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                            {
                                using(var linePen = new System.Drawing.Pen(lineBrush) { Width = 1 })
                                {
                                    graphics.DrawPolygon(linePen, points);
                                }
                            }


                            annotation.bottom = bottomy;
                        }

                        if (position.CloseBar == bar)
                        {
                            double topy = annotation.top;
                            double bottomy = topy + candleWidth * Math.Sqrt(3.0);
                            double topx = BarToX(position.CloseBar);
                            double leftx = topx - candleWidth / 2.0;
                            double rightx = leftx + candleWidth;

                            System.Drawing.Point[] points =
                            {
                            new System.Drawing.Point((int) topx, (int) topy),
                            new System.Drawing.Point((int) rightx, (int) bottomy),
                            new System.Drawing.Point((int) leftx, (int) bottomy),
                            new System.Drawing.Point((int) topx, (int) topy)
                            };

                            using (var fillBrush = new System.Drawing.SolidBrush(
                                position.ClosePrice < position.OpenPrice ?
                                    System.Drawing.Color.Red
                                : position.ClosePrice > position.OpenPrice ? System.Drawing.Color.Green
                                : System.Drawing.Color.FromArgb(0xa6,0xa6,0xa6)
                                ))
                            {
                                graphics.FillPolygon(fillBrush, points);
                            }

                            using (var lineBrush = new System.Drawing.SolidBrush(
                                position.ClosePrice < position.OpenPrice ?
                                    System.Drawing.Color.FromArgb(0x4d, 0x00, 0x00)
                                : position.ClosePrice > position.OpenPrice ? System.Drawing.Color.FromArgb(0x00, 0x4d, 0x00)
                                : System.Drawing.Color.Black
                                ))
                            {
                                using (var linePen = new System.Drawing.Pen(lineBrush) { Width = 1 })
                                {
                                    graphics.DrawPolygon(linePen, points);
                                }
                            }


                            annotation.top = bottomy;
                        }
                    }
                }
            }
        }


        protected virtual void WhiteBoard()
        {

            if( backgroundImage == null )
            {
                backgroundImage = new System.Drawing.Bitmap((int)canvas.ActualWidth, (int)canvas.ActualHeight);
            }
            if( backgroundImage.Width != canvas.ActualWidth || backgroundImage.Height != canvas.ActualHeight )
            {
                backgroundImage.Dispose();
                backgroundImage = new System.Drawing.Bitmap((int)canvas.ActualWidth, (int)canvas.ActualHeight);
            }

            using (var graphics = System.Drawing.Graphics.FromImage(backgroundImage))
            {
                using (var white = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                {
                    graphics.FillRectangle(white, 0, 0, backgroundImage.Width, backgroundImage.Height);
                }
            }

            canvas.Children.Clear();
            // canvas.Background = new SolidColorBrush(Colors.White);
        }

        virtual protected void DrawBottomBar()
        {
            /*
            Line line = new Line();
            var color = MediaColor(System.Drawing.Color.FromArgb(38,38,191));
            line.Stroke = new SolidColorBrush(color); // new Color() { R = 38, G = 38, B = 191 }
            line.StrokeThickness = bottomBarHeight;
            line.X1 = 0;
            line.X2 = canvas.ActualWidth;
            line.Y1 = canvas.ActualHeight-bottomBarHeight / 2;
            line.Y2 = line.Y1;
            // Canvas.SetLeft(rect, canvas.ActualHeight);
            // Canvas.SetBottom(rect, canvas.ActualHeight - bottomBarHeight);
            canvas.Children.Add(line);
            */

            using (var graphics = System.Drawing.Graphics.FromImage(backgroundImage))
            {
                using (var purple = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(38, 38, 191)))
                {
                    graphics.FillRectangle(purple, 0, backgroundImage.Height-bottomBarHeight, backgroundImage.Width, backgroundImage.Height);
                }
            }
        }

        virtual protected void DrawLabels()
        {
#if __APPLEOS__
            var context = NSGraphicsContext.CurrentContext.GraphicsPort;
            context.SaveState();
#endif
            foreach (Pane pane in TraderScript.Panes)
            {
                double y = ValueToY(pane,pane.max) + 5;
                foreach (ColorString label in pane.PaneLabels)
                {
#if __APPLEOS__
                    context.SetStrokeColor(new CGColor(label.Color.R / 255.0f, label.Color.B / 255.0f, label.Color.B / 255.0f));
                    NSAttributedString extents = new NSAttributedString(label.Text);
                    CGPoint point = new CGPoint(
                        (float)5,
                        (float)y + extents.Size.Height
                    );
                    extents.DrawString(point);
                    y += extents.Size.Height + 5;
#endif
#if __WINDOWS__
                    Size extents = TextExtents(label.Text,FontWeights.Normal,12.0);
                    TextBlock textBlock = new TextBlock() { Text = label.Text, Foreground = new SolidColorBrush(MediaColor(label.Color)) };
                    Canvas.SetLeft(textBlock, 5);
                    Canvas.SetTop(textBlock, y);
	                canvas.Children.Add(textBlock);
					y += extents.Height + 5;
#endif
                }
            }
#if __MACOS__
            context.RestoreState();
#endif
        }

        virtual protected void ShowLastValues()
        {
            // Cairo.Context context = Gdk.CairoHelper.Create(drawingarea.GdkWindow);
            // Show the values of the most recent items
            foreach (Pane pane in TraderScript.Panes)
            {
                if (pane != TraderScript.PricePane)
                {
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

                        TextBlock text = new TextBlock();
                        Size valextents = TextExtents(valstring, FontWeights.Normal, 12.0f);
                        text.Text = valstring;
                        text.Foreground = new SolidColorBrush(Colors.White);
                        Canvas.SetLeft(text, actualWidth - rightBorderWidth);
                        Canvas.SetBottom(text, actualHeight - (y + valextents.Height / 2));
                        canvas.Children.Add(text);
                    }
                }
            }
        }

        virtual protected void ShowVolumePriceValues()
        {
            // Cairo.Context context = Gdk.CairoHelper.Create(drawingarea.Window);
            // For each pane draw lines to show values
            try
            {
                foreach (Pane pane in new List<Pane>() { TraderScript.VolumePane, TraderScript.PricePane })
                {
                    Size textextents = TextExtents("0.123456789", FontWeights.Normal, 12.0f);
                    double range = (pane.max - pane.min);
                    double maxnovalues = pane.size * height / totalSize / (textextents.Height * 4);
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
                        double y = ValueToY(pane,val);

                        string valstring;
                        if (Math.Abs(val) < 1000)
                            valstring = string.Format("{0:#.##}", val);
                        else if (Math.Abs(val) < 100000)
                            valstring = val.ToString();
                        else
                            valstring = string.Format("{0:0.0000e0}", val);

                        TextBlock text = new TextBlock();
                        text.Foreground = new SolidColorBrush(Colors.White);

                        Size valextents = TextExtents(valstring, FontWeights.Normal, 12.0f);
                        text.Text = valstring;
                        Canvas.SetLeft(text, width - rightBorderWidth);
                        Canvas.SetBottom(text, actualHeight - (y + valextents.Height / 2));
                        canvas.Children.Add(text);

                        Line line = new Line();
                        line.StrokeThickness = 1;
                        line.Stroke = new SolidColorBrush(new Color() { R = 191, G = 191, B = 191 });
                        line.X1 = 0;
                        line.Y1 = actualHeight - y;
                        line.X2 = canvas.ActualWidth-rightBorderWidth;
                        line.Y2 = canvas.ActualHeight - y;
                        canvas.Children.Add(line);
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


        protected void DrawPaneSeparators()
        {
            int origin = (int) bottomBarHeight;
            foreach (Pane pane in TraderScript.Panes)
            {
                pane.origin = origin;
                origin += pane.size * (int) (canvas.ActualHeight - bottomBarHeight) / totalSize;
                DrawLine(width, origin);
            }
        }

        virtual protected void DrawMonthLines()
        {
            // Cairo.Context context = Gdk.CairoHelper.Create(drawingarea.GdkWindow);
            // Draw month bars and months
            int currentmonth = 0;

            try
            {
                for (int i = (int)FirstBar; i <= LastBar; i++)
                {
                    DateTime date = TraderScript.bars.Date[i];

                    if (date.Month != currentmonth && date.Day <= 7)
                    {
                        double x = BarToX(i);
                        Line line = new Line();
                        // line.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(a:255,r: 191, g: 191, b: 191));
                        line.Stroke = Brushes.LightGray;
                        line.StrokeThickness = 1;
                        line.X1 = x;
                        line.Y1 = 0;
                        line.X2 = x;
                        line.Y2 = height;
                        canvas.Children.Add(line);
                        currentmonth = date.Month;

                        string datestring = date.Day + "/" + date.Month + "/" + date.Year;
                        TextBlock text = new TextBlock();
                        text.FontSize = 12.0;
                        text.Foreground = Brushes.White;
                        text.FontFamily = new FontFamily(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name);
                        text.Text = datestring;
                        Size textextents = TextExtents(datestring, FontWeights.Normal, 12.0);
                        Canvas.SetLeft(text, x - textextents.Width / 2);
                        Canvas.SetBottom(text, (bottomBarHeight - textextents.Height) / 2);
                        canvas.Children.Add(text);
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

        virtual protected void DrawBarBackgrounds()
        {
            // Cairo.Context context = Gdk.CairoHelper.Create(drawingarea.GdkWindow);
            // Draw bar backgrounds.  We want everything else to draw over the top of these
            // that's why they're first

            for (int bar = FirstBar; bar <= LastBar; bar++)
            {
                Annotation annotation = TraderScript.Annotations[bar];
                if (annotation != null)
                {
                    var brush = new System.Drawing.SolidBrush(annotation.backgroundcolor);
                    var pen = new System.Drawing.Pen(brush);
                    pen.Width = (float) candleWidth * 2F;

                    var x = (float) BarToX(bar);
                    using(var graphics = System.Drawing.Graphics.FromImage(backgroundImage))
                    {
                        graphics.DrawLine(pen,x ,0,x, (float) canvas.ActualHeight- bottomBarHeight);
                    }
                }
            }
        }

        virtual protected void DrawDividendBackgrounds()
        {
        }

        virtual protected void DrawLine(int width, int height)
        {
            // Cairo.Context context = Gdk.CairoHelper.Create(drawingarea.GdkWindow);	
            Line line = new Line();
            // context.SetSourceRGB(0.0, 0.0, 0.0);
            line.Stroke = new SolidColorBrush(Colors.Black);
            line.StrokeThickness = 1;
            line.X1 = 0;
            line.Y1 = canvas.ActualHeight - height;
            line.X2 = width;
            line.Y2 = line.Y1;
            canvas.Children.Add(line);
        }

        void CalculatePaneMaximaMinima()
        {
            // Calculate minima and maxima for each pane
            foreach (Pane pane in TraderScript.Panes)
            {
                if (pane.DataSeriess.Count > 0)
                {
                    if (pane.HasRange)
                    {
                        pane.min = pane.InitialMin;
                        pane.max = pane.InitialMax;
                    }
                    else
                    {
                        pane.min = pane.DataSeriess[0][(int)FirstBar];
                        pane.max = pane.DataSeriess[0][(int)FirstBar];
                    }
                    foreach (DataSeries dataseries in pane.DataSeriess)
                    {
                        for (int i = FirstBar; i <= LastBar; i++)
                        {
                            if (dataseries[i] < pane.min)
                                pane.min = dataseries[i];
                            if (dataseries[i] > pane.max)
                                pane.max = dataseries[i];
                        }
                    }
                }
                else
                {
                    pane.min = 0;
                    pane.max = 0;
                }
            }
        }


        virtual protected void DrawTrendLines()
        {
            foreach (Data.TrendLine line in traderBook.TrendLines)
            {
                switch (line.TrendKind)
                {
                    case Data.TrendKind.Linear:
                        DrawLinearTrendLine(line);
                        break;
                    case Data.TrendKind.Quadratic:
                        DrawQuadraticTrendLine(line);
                        break;
                    default:
                        break;
                }
            }
        }

        virtual protected void DrawHandAnnotations()
        {

        }
        virtual protected void DrawLinearTrendLine(Data.TrendLine line)
        {

            int startBar = 0;
            bool foundStart = false;
            for (int i = Bars.Count - 1; i >= 0; i--)
            {
                if (line.StartDate.Date == Bars.Date[i])
                {
                    foundStart = true;
                    startBar = i;
                    break;
                }
            }
            if (!foundStart)
                return;

            int endBar = 0;
            bool foundEnd = false;
            for (int i = 0; i < Bars.Count; i++)
            {
                if (line.EndDate.Date == Bars.Date[i])
                {
                    foundEnd = true;
                    endBar = i;
                    break;
                }
            }
            if (!foundEnd)
                return;

            // Find the slope and intercept. This needs to be done first, so we get the line right.
            double slope = (line.EndPrice - line.StartPrice) / (endBar - startBar);
            double intercept = line.StartPrice - slope * startBar;

            // Contain onto the view
            if (startBar > endBar)
            {
                if (startBar < FirstBar)
                    return;
                if (endBar > LastBar)
                    return;
                if (startBar > LastBar)
                    startBar = LastBar;
                if (endBar < FirstBar)
                    endBar = FirstBar;
            }
            else
            {
                if (startBar > LastBar)
                    return;
                if (endBar < FirstBar)
                    return;
                if (endBar > LastBar)
                    endBar = LastBar;
                if (startBar < FirstBar)
                    startBar = FirstBar;
            }


            // Calculate the prices
            int startPrice = (int) (startBar * slope + intercept);
            int endPrice = (int) (endBar * slope + intercept);

            var wpfLine = new System.Windows.Shapes.Line();
            wpfLine.X1 = BarToX(startBar);
            wpfLine.Y1 = ValueToY(TraderScript.PricePane, startPrice);
            wpfLine.X2 = BarToX(endBar);
            wpfLine.Y2 = ValueToY(TraderScript.PricePane, endPrice);
            wpfLine.Tag = line;
            /*
            using ( var pen = new System.Drawing.Pen(System.Drawing.Color.Black) )
            {
                var startPt = new System.Drawing.Point((int) BarToX(startBar), (int) ValueToY(TraderScript.PricePane, startPrice));
                var endPt = new System.Drawing.Point((int) BarToX(endBar), (int) ValueToY(TraderScript.PricePane, endPrice));
                pen.Width = 1;
                graphics.DrawLine(pen,startPt,endPt);
            }
            */
            canvas.Children.Add(wpfLine);
        }

        virtual protected void DrawQuadraticTrendLine(Data.TrendLine line)
        {

            int startBar = 0, midBar = 0, endBar = 0;

            for (int i = 0; i < Bars.Count; i++)
            {
                DateTime date = Bars.Date[i];
                if (line.StartDate.Date == date.Date)
                    startBar = i;
                if (line.MidDate.Date == date.Date)
                    midBar = i;
                if (line.MidDate.Date == date.Date)
                    midBar = i;
                if (line.EndDate.Date == date.Date)
                    endBar = i;
            }

            var startPt = new Point( BarToX(startBar), ValueToY(TraderScript.PricePane, line.StartPrice));
            var midPt = new Point( BarToX(midBar), ValueToY(TraderScript.PricePane, line.MidPrice));
            var endPt = new Point( BarToX(endBar), ValueToY(TraderScript.PricePane, line.EndPrice));

            var path = new System.Windows.Shapes.Path() { Stroke=Brushes.Black, StrokeThickness = 1 };
            path.Tag = line;
            var pathGeometry = new PathGeometry();
            path.Data = pathGeometry;
            var segment = new QuadraticBezierSegment(midPt, endPt, true);
            var segmentCollection = new PathSegmentCollection() { segment };
            var pathFigure = new PathFigure(startPt,segmentCollection,false);

            pathGeometry.Figures = new PathFigureCollection() { pathFigure };
            canvas.Children.Add(path);
        }



        virtual protected void DrawAnnotations()
        {
            for (int bar = FirstBar; bar <= LastBar; bar++)
            {
                Annotation annotation = TraderScript.Annotations[bar];
                foreach (Position position in annotation.position)
                {
                    if (position.OpenBar == bar)
                    {
                        TextBlock text = new TextBlock();
                        text.Text = position.OpenSignal;
                        text.Foreground = new SolidColorBrush(Colors.Black);
                        text.FontSize = 12.0;
                        text.FontWeight = FontWeights.Normal;
                        text.FontFamily = new FontFamily(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name);
                        // context.SetSourceRGB( 0.0, 0.0, 0.0 );
                        Size extents = TextExtents(position.OpenSignal, FontWeights.Normal, 12.0);
                        Canvas.SetLeft(text, BarToX(bar) - extents.Width / 2.0);
                        Canvas.SetBottom(text, annotation.bottom + extents.Height * 1.2);
                        // context.MoveTo( BarToX(bar) - extents.Width/2.0, annotation.bottom + extents.Height*1.2 );
                        // context.ShowText( position.OpenSignal );	
                        canvas.Children.Add(text);
                        annotation.bottom += extents.Height * 1.2;
                    }
                    if (position.CloseBar == bar)
                    {
                        TextBlock text = new TextBlock();
                        text.Text = position.OpenSignal;
                        text.Foreground = new SolidColorBrush(Colors.Black);
                        text.FontSize = 12.0;
                        text.FontWeight = FontWeights.Normal;
                        text.FontFamily = new FontFamily(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name);

                        // context.SetSourceRGB( 0.0, 0.0, 0.0 );
                        Size extents = TextExtents(position.CloseSignal, FontWeights.Normal, 12.0);
                        Canvas.SetLeft(text, BarToX(bar) - extents.Width / 2.0);
                        Canvas.SetBottom(text, annotation.top - extents.Height * 0.2);
                        // context.MoveTo( BarToX(bar) - extents.Width/2.0, annotation.top - extents.Height*0.2 );
                        // context.ShowText( position.CloseSignal );
                        canvas.Children.Add(text);
                        annotation.top -= extents.Height * 1.2;
                    }
                }

                // Cairo.FontFace defaultface = context.ContextFontFace;
                // Cairo.Matrix defaultmatrix = context.FontMatrix;
                // Cairo.FontOptions defaultoptions = context.FontOptions;

                foreach (ColorString colorstring in annotation.colorstring)
                {
                    TextBlock text = new TextBlock();
                    text.Foreground = new SolidColorBrush(new Color() { R = colorstring.Color.R, B = colorstring.Color.B, G = colorstring.Color.G });
                    // context.SetSourceRGB( colorstring.Color.R/255.0, colorstring.Color.B/255.0, colorstring.Color.B/255.0 );
                    if (colorstring.Font != null)
                    {
                        text.FontWeight = (colorstring.Font.Bold ? FontWeights.Bold : FontWeights.Normal);
                        text.FontStyle = (colorstring.Font.Italic ? FontStyles.Italic : FontStyles.Normal);
                        text.FontSize = (double)colorstring.Font.Size;
                        text.FontFamily = new FontFamily(colorstring.Font.FontFamily.Name);
                    }
                    else
                    {
                        // context.ContextFontFace = defaultface;
                        // context.FontOptions = defaultoptions;
                        // context.FontMatrix = defaultmatrix;
                    }
                    Size extents = TextExtents(colorstring.Text,text.FontWeight,text.FontSize);
                    Canvas.SetLeft(text, BarToX(bar) - extents.Width / 2.0);
                    Canvas.SetBottom(text, annotation.top - extents.Height * 0.2);
                    // context.MoveTo( BarToX(bar) - extents.Width/2.0, annotation.top - extents.Height*0.2 );
                    // context.ShowText(colorstring.Text);	
                    annotation.top -= extents.Height * 1.2;
                    canvas.Children.Add(text);
                }

                // context.ContextFontFace = defaultface;
                // context.FontOptions = defaultoptions;
                // context.FontMatrix = defaultmatrix;
            }
        }


        public void ExecuteMethods()
        {
            bool isBehind = true;
            do
            {
                foreach (DrawingItem item in TraderScript.DrawingItems)
                {
                    if (item.behind == isBehind)
                    {
                        double paneHeight = height * item.pane.size / totalSize;
                        switch (item.drawingmethod)
                        {
                            case DrawingMethod.DrawHorzLine:
                                executeDrawHorzLine(item.pane, width, paneHeight, item.parameters);
                                break;
                            case DrawingMethod.PlotPrices:
                                executePlotPrices(item.pane, width, paneHeight, item.parameters);
                                break;
                            case DrawingMethod.PlotHistogram:
                                executePlotHistogram(item.pane, width, paneHeight, item.parameters);
                                break;
                            case DrawingMethod.PlotPattern:
                                executePlotPattern(item.pane, width, paneHeight, item.parameters);
                                break;
                            case DrawingMethod.PlotSeries:
                                executePlotSeries(item.pane, width, paneHeight, item.parameters);
                                break;
                            case DrawingMethod.PlotSeriesOscillator:
                                executePlotSeriesOscillator(item.pane, width, paneHeight, item.parameters);
                                break;
                            case DrawingMethod.PlotSeriesFillBand:
                                executePlotSeriesFillBand(item.pane, width, paneHeight, item.parameters);
                                break;
                            case DrawingMethod.DrawPolygon:
                                executeDrawPolygon(item.pane, width, paneHeight, item.parameters);
                                break;
                            case DrawingMethod.DrawLine:
                                executeDrawLine(item.pane, width, paneHeight, item.parameters);
                                break;
                            default:
                                break;
                        }
                    }
                }
                isBehind = !isBehind;
            } while(!isBehind);
        }

        /* FIX_LATER
		void DrawingArea_MotionNotifyEvent(object o, MotionNotifyEventArgs args) 
		{
			double x = args.Event.X;
			// double y = args.Event.Y; this is never used
			int i = (int) Math.Round((x - mCandleWidth)/(mCandleWidth*2)+mAdjustment.Value);
			if( parent.TraderScript != null && parent.mDataFile != null )
			{
				if( i >= 0 && i < parent.bars.Count )
				{
					DateTime date = parent.bars.Date[i];
					string datestring = date.Day.ToString() + "/" + date.Month.ToString() + "/" + date.Year.ToString();
					mPositionDateLabel.Text = "Date: " + datestring;
					try
					{
						mPositionOpenLabel.Text = "Open: " + parent.bars.Open[i].ToString();
					} catch( Exception debugException )
					{
						System.Diagnostics.StackTrace debugStack = new System.Diagnostics.StackTrace( debugException, true );
						string debugLineNumber = debugStack.GetFrame(0).GetFileLineNumber().ToString();
						string debugMessage = "(" + debugLineNumber + ") " + debugException.Message;
						Gtk.MessageDialog debugDialog = new Gtk.MessageDialog( null, Gtk.DialogFlags.Modal, Gtk.MessageType.Info, Gtk.ButtonsType.Ok, debugMessage, new object[] { (object) "" } );	
						debugDialog.Show();							
					}
					
					mPositionHighLabel.Text = "High: " + parent.bars.High[i].ToString();
					mPositionLowLabel.Text = "Low: " + parent.bars.Low[i].ToString();
					mPositionCloseLabel.Text = "Close: " + parent.bars.Close[i].ToString();
					mPositionVolumeLabel.Text = "Volume: " + parent.bars.Volume[i].ToString();
				}
			}
			else
				mPositionDateLabel.Text = "";
		}
		*/

        void DrawingArea_SizeAllocated(object o, SizeChangedEventArgs args)
        {
            if (hScrollbar != null)
            {
                Canvas canvas = o as Canvas;

                double canvasWidth = canvas.Width;
                canvas.Width = canvasWidth;
                double pagesize = Math.Floor(((canvasWidth - rightBorderWidth) / (candleWidth * 2)));
                if (pagesize < 1)
                    pagesize = 1;
                hScrollbar.LargeChange = pagesize;

                if (hScrollbar.Value + pagesize - 1 > hScrollbar.Maximum)
                {
                    hScrollbar.Value = hScrollbar.Maximum - pagesize + 1;
                }
            }
        }


        void DrawingArea_ExposeEvent(object sender, EventArgs args)
        {
            // UpdateLayout();
        }

        protected object lockGraphic = new object();

        protected virtual void executePlotPrices(Pane pane, double width, double height, object[] parameters)
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
                int lastBar = LastBar + 1;
                using (var graphics = System.Drawing.Graphics.FromImage(backgroundImage))
                {
                    using (var purple = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(38, 38, 191)))
                    {
                        ParallelOptions options = new ParallelOptions();
                        options.MaxDegreeOfParallelism = processorCount;

                        Parallel.For(firstBar, lastBar + 1, options, i =>
                        {
                             double x = BarToX(firstBar, i);
                             System.Drawing.Brush color;
                             double open = bars.Open[i];
                             double close = bars.Close[i];
                             double low = bars.Low[i];
                             double high = bars.High[i];

                             if (open > close)
                                 color = new System.Drawing.SolidBrush(System.Drawing.Color.Red); //new Color() { R = 179, G = 0, B = 0, A=0 };
                            else if (open == close)
                                 color = new System.Drawing.SolidBrush(System.Drawing.Color.Black); //new Color() { R = 0, G = 0, B = 0, A = 1 };
                            else
                                 color = new System.Drawing.SolidBrush(System.Drawing.Color.Green); // new Color() { R = 0, G = 179, B = 0, A = 1 };


                            var wick = new System.Drawing.Pen(color, 1F);
                            var body = new System.Drawing.Pen(color, open != close ? (float)candleWidth : 1);
                            lock (lockGraphic)
                            {
                                graphics.DrawLine(wick, (int)(x + candleWidth / 2), (int)ValueToY(Math.Max(open, close)), (int)(x + candleWidth / 2), (int)ValueToY(high));
                                if( open == close )
                                    graphics.DrawLine(body, (int) x, (int)ValueToY(Math.Min(open, close)), (int)(x + candleWidth ), (int)ValueToY(Math.Max(open, close)));
                                else
                                    graphics.DrawLine(body, (int)(x + candleWidth / 2), (int)ValueToY(Math.Min(open, close)), (int)(x + candleWidth / 2), (int)ValueToY(Math.Max(open, close)));
                                graphics.DrawLine(wick, (int)(x + candleWidth / 2), (int)ValueToY(Math.Min(open, close)), (int)(x + candleWidth / 2), (int)ValueToY(low));
                            }
                             // System.Diagnostics.Debug.WriteLine(i.ToString() + " " + x.ToString() + "," + pane.chart.ValueToY(pane, bars.Close[i]).ToString());
                         });
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

        protected virtual void executePlotPattern(Pane pane, double width, double height, object[] parameters)
        {

            var bars = (int[]) parameters[0];
            var thickness = (int)parameters[3];
            var lineStyle = (LineStyle)parameters[2];
            var color = (System.Drawing.Color) parameters[2];

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
                using (var graphics = System.Drawing.Graphics.FromImage(backgroundImage))
                {
                    using (var brush = new System.Drawing.SolidBrush(color))
                    {
                        using (var pen = new System.Drawing.Pen(brush))
                        {
                            pen.Width = thickness;
                            pen.DashStyle = lineStyle switch { 
                                LineStyle.Dashes => System.Drawing.Drawing2D.DashStyle.Dash, 
                                LineStyle.Dots => System.Drawing.Drawing2D.DashStyle.Dot,
                                _ => System.Drawing.Drawing2D.DashStyle.Solid
                            };

                            for (int i = 1; i < bars.Length; i++)
                            {
                                double x1 = BarToX(bars[i - 1]);
                                double y1 = ValueToY(Bars.Close[i - 1]);
                                double x2 = BarToX(bars[i]);
                                double y2 = ValueToY(Bars.Close[i]);

                                graphics.DrawLine(pen, (int)x1, (int)y1, (int)x2, (int)y2);
                            }
                        }
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

        public void executePlotPrices_old(Pane pane, double width, double height, object[] parameters)
        {

            Bars bars = (Bars)parameters[0];

            try
            {
                for (int i = pane.chart.FirstBar; i <= pane.chart.LastBar; i++)
                {
                    double x = pane.chart.BarToX(i);

                    Color color;
                    double open = bars.Open[i];
                    double close = bars.Close[i];
                    double low = bars.Low[i];
                    double high = bars.High[i];

                    if (bars.Open[i] > bars.Close[i])
                        color = MediaColor(System.Drawing.Color.Red); //new Color() { R = 179, G = 0, B = 0, A=0 };
                    else if (bars.Open[i] == bars.Close[i])
                        color = MediaColor(System.Drawing.Color.Black); //new Color() { R = 0, G = 0, B = 0, A = 1 };
                    else
                        color = MediaColor(System.Drawing.Color.Green); // new Color() { R = 0, G = 179, B = 0, A = 1 };

                    var body = new Line();
                    body.Stroke = new SolidColorBrush(color);
                    body.StrokeThickness = candleWidth;
                    body.X1 = x + candleWidth / 2;
                    body.X2 = body.X1;
                    body.Y1 = ValueToY(pane, Math.Max(open, close));
                    body.Y2 = ValueToY(pane, Math.Min(open, close));

                    var top = new Line();
                    top.Stroke = new SolidColorBrush(color);
                    top.StrokeThickness = 1;
                    top.X1 = x + candleWidth / 2; ;
                    top.X2 = top.X1;
                    top.Y1 = body.Y1;
                    top.Y2 = pane.chart.ValueToY(pane, high);


                    var bottom = new Line();
                    bottom.Stroke = new SolidColorBrush(color);
                    bottom.StrokeThickness = 1;
                    bottom.X1 = x + candleWidth / 2; ;
                    bottom.X2 = bottom.X1;
                    bottom.Y1 = body.Y2;
                    bottom.Y2 = pane.chart.ValueToY(pane, low);


                    canvas.Children.Add(top);
                    canvas.Children.Add(body);
                    canvas.Children.Add(bottom);
                    // int indexPosition = pane.drawingarea.Children.Add(body);
                    // pane.drawingarea.Children.Add(bottom);

                    System.Diagnostics.Debug.WriteLine(i.ToString() + " " + x.ToString() + "," + pane.chart.ValueToY(pane, bars.Close[i]).ToString());
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

        protected virtual void executePlotHistogram(Pane pane, double width, double height, object[] parameters)
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

            double max = pane.max;
            double min = pane.min;

            double range = max - min;
            if (range == 0) range = 1;

            try
            {
                ParallelOptions options = new ParallelOptions();
                options.MaxDegreeOfParallelism = processorCount;

                Parallel.For(firstBar, lastBar + 1, options, i =>
                 {
                     lock (lockGraphic)
                     {
                         using (var graphics = System.Drawing.Graphics.FromImage(backgroundImage))
                         {
                             using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B)))
                             {
                                 using (var pen = new System.Drawing.Pen(brush))
                                 {
                                     pen.Width = thickness;
                                     graphics.DrawLine(pen,
                                         (int)BarToX(i),
                                         (int)ValueToY(ds[i]),
                                         (int)BarToX(i),
                                         (int)actualHeight - paneOrigin
                                         );
                                 }
                             }
                         }
                     }
                 });
            }
            catch (Exception debugException)
            {
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(debugException, true);
                string stringLineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
                string message = "(" + stringLineNumber + ") " + debugException.Message;
                MessageBox.Show(message);
            }
        }

        protected virtual void executePlotSeries(Pane pane, double width, double height, object[] parameters)
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

            // Cairo.Context context = Gdk.CairoHelper.Create(pane.drawingarea.GdkWindow);
            // context.Save();
            // context.Rectangle( 0, pane.origin-height, width, height );
            // context.Clip();

            try
            {
                //context.Translate(0, 0); // Move the origin to the bottom

                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B)))
                {
                    using (var pen = new System.Drawing.Pen(brush))
                    {
                        pen.Width = thickness;
                        pen.DashStyle = linestyle switch
                        {
                            LineStyle.Dots => System.Drawing.Drawing2D.DashStyle.Dot,
                            LineStyle.Dashes => System.Drawing.Drawing2D.DashStyle.Dash,
                            _ => System.Drawing.Drawing2D.DashStyle.Solid
                        };

                        ParallelOptions options = new ParallelOptions();
                        options.MaxDegreeOfParallelism = processorCount;
                        Parallel.For(firstBar , lastBar+1 , options,  i =>
                          {
                              lock (lockGraphic)
                              {
                                  using (var graphics = System.Drawing.Graphics.FromImage(backgroundImage))
                                  {
                                      double y1 = ValueToY(ds[i]);
                                      double x1 = BarToX(i);
                                      double x2 = BarToX(i+1);
                                      double y2 = ValueToY(ds[i+1]);

                                      graphics.DrawLine(pen, (int)x1, (int)y1, (int)x2, (int)y2);
                                  }
                              }
                          });
                    }
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

        public void executePlotSeries_old(Pane pane, double width, double height, object[] parameters)
        {

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

            LineStyle linestyle = (LineStyle)parameters[2];
            int thickness = (int)parameters[3];

            if (linestyle == LineStyle.Histogram)
            {
                executePlotHistogram(pane, width, height, new object[] { (object)ds, (object)color, (object)thickness });
                return;
            }

            // Cairo.Context context = Gdk.CairoHelper.Create(pane.drawingarea.GdkWindow);
            // context.Save();
            // context.Rectangle( 0, pane.origin-height, width, height );
            // context.Clip();

            try
            {
                //context.Translate(0, 0); // Move the origin to the bottom


                double y1 = pane.chart.ValueToY(pane, ds[pane.chart.FirstBar]);
                double x1 = pane.chart.BarToX(pane.chart.FirstBar);


                for (int i = pane.chart.FirstBar + 1; i <= pane.chart.LastBar; i++)
                {
                    double x2 = pane.chart.BarToX(i);
                    double y2 = pane.chart.ValueToY(pane, ds[i]);
                    Line line = new Line();
                    line.Stroke = new SolidColorBrush(color);
                    line.StrokeThickness = thickness;
                    line.X1 = x1;
                    line.Y1 = y1;
                    line.X2 = x2;
                    line.Y2 = y2;
                    canvas.Children.Add(line);
                    x1 = x2;
                    y1 = y2;
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

        virtual protected void executePlotSeriesFillBand(Pane pane, double width, double height, object[] parameters)
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


                Parallel.For(firstBar, lastBar + 1, i =>
                {
                    double upper_y = ValueToY(ds1[i]);
                    double x = BarToX(i);


                    double lower_y = ValueToY(ds2[i]);


                    double lower_y1 = ValueToY(ds2[i + 1]);
                    double x1 = BarToX(i + 1);


                    double upper_y1 = ValueToY(ds1[i + 1]);

                    lock (lockGraphic)
                    {
                        using (var graphics = System.Drawing.Graphics.FromImage(backgroundImage))
                        {
                            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(bandcolor.A, bandcolor.R, bandcolor.G, bandcolor.B)))
                            {
                                System.Drawing.Point[] points = {
                                new System.Drawing.Point((int) x, (int) upper_y),
                                new System.Drawing.Point((int) x, (int) lower_y),
                                new System.Drawing.Point((int) x1, (int) lower_y1),
                                new System.Drawing.Point((int) x1, (int) upper_y1)
                            };
                                graphics.FillPolygon(brush, points);
                            }
                        }
                    }
                });

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


        public void executePlotSeriesFillBand_old(Pane pane, double width, double height, object[] parameters)
        {
            try
            {
                DataSeries ds1 = (DataSeries)parameters[0];
                DataSeries ds2 = (DataSeries)parameters[1];
                Color linecolor = MediaColor((System.Drawing.Color)parameters[2]);
                Color bandcolor = MediaColor((System.Drawing.Color)parameters[3]);
                LineStyle linestyle = (LineStyle)parameters[4];
                int thickness = (int)parameters[5];

                if (linecolor != Colors.Transparent)
                {
                    object[] series1parameters = new object[] { (object)ds1, (object)linecolor, (object)linestyle, (object)thickness };
                    object[] series2parameters = new object[] { (object)ds2, (object)linecolor, (object)linestyle, (object)thickness };
                    executePlotSeries(pane, width, height, series1parameters);
                    executePlotSeries(pane, width, height, series2parameters);
                }

                // context.Translate(0, 0); // Move the origin to the bottom

                double max = pane.max;
                double min = pane.min;
                int origin = pane.origin;

                // context.SetSourceRGBA(bandcolor.R/255.0, bandcolor.G/255.0, bandcolor.B/255.0, (bandcolor.A/255.0));			
                // context.LineWidth = thickness;


                for (int i = pane.chart.FirstBar; i < pane.chart.LastBar; i++)
                {
                    Polyline polyline = new Polyline();
                    polyline.StrokeThickness = thickness;
                    polyline.Stroke = new SolidColorBrush(bandcolor);
                    polyline.Fill = new SolidColorBrush(bandcolor);

                    double upper_y = pane.chart.ValueToY(pane, ds1[i]);
                    double x = pane.chart.BarToX(i);
                    polyline.Points.Add(new Point(x, upper_y));
                    // context.MoveTo( x, upper_y );

                    double lower_y = pane.chart.ValueToY(pane, ds2[i]);
                    polyline.Points.Add(new Point(x, lower_y));
                    // context.LineTo( x, lower_y);

                    double lower_y1 = pane.chart.ValueToY(pane, ds2[i + 1]);
                    double x1 = pane.chart.BarToX(i + 1);
                    polyline.Points.Add(new Point(x1, lower_y1));
                    // context.LineTo( x1, lower_y1 );

                    double upper_y1 = pane.chart.ValueToY(pane, ds1[i + 1]);
                    polyline.Points.Add(new Point(x1, upper_y1));
                    // context.LineTo( x1, upper_y1 );

                    // context.LineTo( x, upper_y);

                    canvas.Children.Add(polyline);
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

        // void PlotSeriesOscillator(ChartPane pane, DataSeries source, double overbought, double oversold, Brush overboughtBrush, Brush oversoldBrush, Color color, LineStyle style, int width);

        protected System.Windows.Media.Color MediaColor(System.Drawing.Color color)
        {
            System.Windows.Media.Color mediaColor = System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
            return mediaColor;
        }

        protected virtual void executePlotSeriesOscillator(Pane pane, double width, double height, object[] parameters)
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
                Parallel.For (firstBar,  lastBar+1, options, i =>
                {


                    if (ds1[i] <= oversold || ds1[i + 1] <= oversold)
                    {
                        double x1 = BarToX(firstBar,i);
                        double y1 = ValueToY(ds1[i]);
                        double x2 = BarToX(firstBar,i + 1);
                        double y2 = ValueToY(ds1[i + 1]);
                        if (ds1[i] > oversold)
                        {
                            double slope = (ValueToY(ds1[i + 1]) - ValueToY(ds1[i])) / (BarToX(firstBar,i + 1) - BarToX(firstBar,i));
                            double intercept = ValueToY(ds1[i]) - slope * BarToX(firstBar,i);
                            x1 = (ValueToY(oversold) - intercept) / slope;
                            y1 = x1 * slope + intercept;
                        }
                        else if (ds1[i + 1] > oversold)
                        {
                            double slope = (ValueToY(ds1[i + 1]) - ValueToY(ds1[i])) / (BarToX(firstBar,i + 1) - BarToX(firstBar,i));
                            double intercept = ValueToY(ds1[i]) - slope * BarToX(firstBar,i);
                            x2 = (ValueToY(oversold) - intercept) / slope;
                            y2 = x2 * slope + intercept;
                        }

                        lock (lockGraphic)
                        {
                            using (var graphics = System.Drawing.Graphics.FromImage(backgroundImage))
                            {
                                var points = new System.Drawing.Point[] {
                                    new System.Drawing.Point((int) x1, (int) y1),
                                    new System.Drawing.Point((int) x1, (int) ValueToY(oversold)),
                                    new System.Drawing.Point((int) x2, (int) ValueToY(oversold)),
                                    new System.Drawing.Point((int) x2, (int) y2)
                                };

                                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(linecolor.A, linecolor.R, linecolor.G, linecolor.B)))
                                {
                                    var pen = new System.Drawing.Pen(brush);
                                    pen.Width = thickness;

                                    graphics.DrawPolygon(pen, points);
                                }

                                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(oversoldColor.A, oversoldColor.R, oversoldColor.G, oversoldColor.B)))
                                {

                                    graphics.FillPolygon(brush, points);
                                }
                            }
                        }
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

                        lock(lockGraphic)
                        { 
                            using (var graphics = System.Drawing.Graphics.FromImage(backgroundImage))
                            {
                                var points = new System.Drawing.Point[] {
                                        new System.Drawing.Point((int) x1, (int) y1),
                                    // context.LineTo( x1, pane.chart.ValueToY( pane,oversold ) );
                                        new System.Drawing.Point((int) x1, (int) ValueToY(overbought)),
                                    // context.LineTo( x2, pane.chart.ValueToY( pane,oversold ) );		
                                        new System.Drawing.Point((int) x2, (int) ValueToY(overbought)),
                                    // context.LineTo( x2, y2 );	
                                        new System.Drawing.Point((int) x2, (int) y2)
                                    // context.LineTo( x1, y1);
                                    };

                                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(overboughtColor.A, overboughtColor.R, overboughtColor.G, overboughtColor.B)))
                                {
                                    graphics.FillPolygon(brush, points);
                                }

                                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(linecolor.A, linecolor.R, linecolor.G, linecolor.B)))
                                {
                                    var pen = new System.Drawing.Pen(brush);
                                    pen.Width = thickness;
                                    graphics.DrawPolygon(pen, points);
                                }


                            }
                        }
                    }
                });
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

        public void executeDrawHorzLine(Pane pane, double width, double height, object[] parameters)
        {
            // DrawingContext context = canvas.ContextMenuOpening ;
            // context.Save();
            // context.Rectangle( 0, pane.origin-height, width, height );
            // context.Clip();

            double val = (double)parameters[0];
            Color color = (Color)parameters[1];
            LineStyle linestyle = (LineStyle)parameters[2];
            int thickness = (int)parameters[3];

            if (val >= pane.min && val <= pane.max)
            {
                Line line = new Line();
                line.Stroke = new SolidColorBrush(color);
                line.Width = thickness;
                line.X1 = 0;
                line.Y1 = line.X2 = pane.chart.ValueToY(pane, val);
                line.X2 = width;
                canvas.Children.Add(line);
            }
        }

        // void DrawLine(ChartPane pane, int bar1, double value1, int bar2, double value2, Color color, LineStyle style, int width);

        public void executeDrawLine(Pane pane, double width, double height, object[] parameters)
        {
            int bar1 = (int)parameters[0];
            double value1 = (double)parameters[1];
            int bar2 = (int)parameters[2];
            double value2 = (double)parameters[3];
            Color color = MediaColor((System.Drawing.Color)parameters[4]);
            LineStyle linestyle = (LineStyle)parameters[5];
            int thickness = (int)parameters[6];

            double minbar = pane.chart.FirstBar;
            double maxbar = pane.chart.LastBar;

            double firstbar = Math.Min(bar1, bar2);
            double lastbar = Math.Max(bar1, bar2);
            if (firstbar > maxbar || lastbar < minbar)
                return;

            double slope = (value2 - value1) / ((double)bar2 - (double)bar1);
            double intercept = value1 - slope * (double)bar1;

            if (firstbar < minbar)
                firstbar = minbar;
            double firstvalue = firstbar * slope + intercept;
            if (firstvalue < pane.min)
            {
                firstbar = (pane.min - intercept) / slope;
                firstvalue = pane.min;
            }
            if (firstvalue > pane.max)
                return;


            if (lastbar > maxbar)
                lastbar = maxbar;
            double lastvalue = lastbar * slope + intercept;
            if (lastvalue > pane.max)
            {
                lastbar = (pane.max - intercept) / slope;
                lastvalue = pane.min;
            }
            if (lastvalue < pane.min)
                return;

            // context.Save();
            // context.Rectangle( 0, pane.origin-height, width, height );
            // context.Clip();

            Line line = new Line();
            // context.SetSourceRGBA(color.R/255.0, color.G/255.0, color.B/255.0, color.A/255.0 );
            line.Stroke = new SolidColorBrush(color);
            line.Width = thickness;

            double max = pane.max;
            double min = pane.min;
            double origin = pane.origin;

            line.Y1 = pane.chart.ValueToY(pane, firstvalue);
            line.X1 = pane.chart.BarToX((int)firstbar);
            // context.MoveTo( x1, y1 );

            line.Y2 = pane.chart.ValueToY(pane, lastvalue);
            line.X2 = pane.chart.BarToX((int)lastbar);
            // context.LineTo( x2, y2 );

            canvas.Children.Add(line);
        }

        // void DrawPolygon(ChartPane pane, Color color, Color fillColor, LineStyle style, int width, bool behindBars, params double[] coords);

        public void executeDrawPolygon(Pane pane, double width, double height, object[] parameters)
        {


            System.Windows.Media.Color color = MediaColor((System.Drawing.Color)parameters[0]);
            System.Windows.Media.Color fillColor = MediaColor((System.Drawing.Color)parameters[1]);
            LineStyle style = (LineStyle)parameters[2];
            int thickness = (int)parameters[3];
            bool behindBars = (bool)parameters[4];
            double[] coords = (double[])parameters[5];

            bool inside = false;
            for (int i = 0; i < coords.Length; i += 2)
            {
                if (coords[i] >= pane.chart.FirstBar && coords[i] <= pane.chart.LastBar
                    && coords[i + 1] >= pane.min && coords[i + 1] <= pane.max)
                {
                    inside = true;
                    break;
                }
            }
            if (!inside)
                return;

            // Cairo.Context context = Gdk.CairoHelper.Create(pane.drawingarea.GdkWindow);
            // context.Save();
            // context.Rectangle( 0, pane.origin-height, width, height );
            // context.Clip();

            Polyline polyline = new Polyline();
            polyline.StrokeThickness = thickness;
            polyline.Stroke = new SolidColorBrush(color);

            // context.MoveTo( pane.chart.BarToX((int) coords[0]), pane.chart.ValueToY(pane,coords[1]) );
            for (int i = 0; i < coords.Length; i += 2)
            {
                polyline.Points.Add(new Point(pane.chart.BarToX((int)coords[i]), pane.chart.ValueToY(pane, coords[i + 1])));
            }
            polyline.Fill = new SolidColorBrush(fillColor);
            canvas.Children.Add(polyline);

        }

        virtual protected void DrawScriptAnnotations()
        {

        }

        virtual protected void DrawAnnotationShapes()
        {

        }

        // void DrawPolygon(ChartPane pane, Color color, Color fillColor, LineStyle style, int width, bool behindBars, params double[] coords);
        public Size TextExtents(string text, FontWeight fontWeight, double fontSize)
        {
            FontFamily fontFamily = new FontFamily(System.Drawing.SystemFonts.DefaultFont.FontFamily.Name);
            FontStyle fontStyle = FontStyles.Normal;
            FontStretch fontStretch = FontStretches.Normal;

            FormattedText ft = new FormattedText(text,
                                                 System.Globalization.CultureInfo.CurrentCulture,
                                                 FlowDirection.LeftToRight,
                                                 new Typeface(fontFamily, fontStyle, fontWeight, fontStretch),
                                                 fontSize,
                                                 Brushes.Black);
            return new Size(ft.Width, ft.Height);
        }


    }

}

