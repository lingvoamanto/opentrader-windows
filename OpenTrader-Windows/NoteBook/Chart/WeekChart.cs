using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;


namespace OpenTrader
{
    public class WeekChart : DirectXChart
    {
        Controls.ChartControl chartControl;
        int barInCanvas;
        double priceInCanvas;

        public WeekChart(TraderBook traderBook, Controls.ChartControl chartControl) : base(traderBook, chartControl)
        {
            this.chartControl = chartControl;

            canvas.MouseMove += Canvas_MouseMove;
        }

        override public int LastBar
        {
            get { return (int)Math.Min(FirstBar + BarsInPage, traderBook.weekBars.Count) - 1; }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (traderBook.DataFile == null)
                return;

            UpdateBarAndPriceInView(e.MouseDevice.GetPosition(canvas));
        }

        void UpdateBarAndPriceInView(Point point)
        {
            double i = XToBar(point.X);
            int count = traderBook.mDataFile.weekBars.Count;
            if (count == 0)
            {
                // traderBook.DayViewController.UpdateDetailsBar((int)i);
                if (canvas.Tag is Controls.ChartControl chartControl)
                {
                    chartControl.UpdateDetails();
                    chartControl.UpdatePrice();
                }
                return;
            }

            System.DateTime date = traderBook.bars.Date[count - 1];
            if (i > LastBar)
                i = LastBar;
            if (i < FirstBar)
                i = FirstBar;
            if (i >= FirstBar && i <= LastBar)
            {
                barInCanvas = (int)i;
                if (canvas.Tag is Controls.ChartControl chartControl)
                {
                    chartControl.UpdateDetails(
                        traderBook.weekBars.Date[barInCanvas],
                        traderBook.weekBars.Open[barInCanvas],
                        traderBook.weekBars.High[barInCanvas],
                        traderBook.weekBars.Low[barInCanvas],
                        traderBook.weekBars.Close[barInCanvas],
                        traderBook.weekBars.Volume[barInCanvas]
                        );
                }
            }

            Pane pricePane = traderBook.TraderScript.PricePane;
            double y = point.Y;
            priceInCanvas = YToPrice(point.Y);

            if (canvas.Tag is Controls.ChartControl chartCtrl)
            {
                chartCtrl.UpdatePrice(priceInCanvas);
            }
        }

        override protected void DrawMonthLines()
        {
            // Cairo.Context context = Gdk.CairoHelper.Create(drawingarea.GdkWindow);
            // Draw month bars and months
            int currentQuarter = 0;
            var bars = TraderScript.bars;

            try
            {
                for (int i = (int)FirstBar; i <= LastBar; i++)
                {
                    DateTime date = bars.Date[i];
                    var quarter = (date.Month - 1) / 4;
                    if (quarter != currentQuarter && date.Day <= 7)
                    {
                        currentQuarter = (date.Month - 1) / 4;

                        double x = BarToX(i);
                        context.DrawLine(lightGrayPen, new Point(x, 0), new Point(x, actualHeight));


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

    }
}
