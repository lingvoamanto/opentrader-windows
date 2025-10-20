using System;
#if __MACOS__
using AppKit;
using CoreGraphics;
#endif
#if __WINDOWS__
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
#endif
using System.Collections.Generic;

namespace OpenTrader
{
#if __MACOS__
    public class ProfitPage : NSTabViewItem, ITraderPage
#endif
#if __WINDOWS__
    public class ProfitPage : TraderPage
#endif
    {
        TraderBook mTraderBook;
#if __MACOS__
        ProfitView mProfitView;
#endif
#if __WINDOWS__
        Canvas canvas;
        ParameterBar parameterBar;
#endif
        Bars mBars;
        List<Position> mPositions;
        public ParameterBar ParameterBar;

        public List<Position> Positions
        {
            set { mPositions = value; }
        }

        public Bars Bars
        {
            set { mBars = value; }
        }

        public PageType PageType
        {
            get { return PageType.Profit; }
        }

        public TraderBook TraderBook
        {
            get { return mTraderBook;  }
            set { mTraderBook = value;  }
        }

#if __MACOS__
        public void Show()
        {
            nint index = mTraderBook.IndexOf(this);
            if (index == nint.MaxValue)
            {
                mTraderBook.Add(this);
            }
        }

        public void Hide()
        {
            nint index = mTraderBook.IndexOf(this);
            if (index != nint.MaxValue)
            {
                mTraderBook.Remove(this);
            }
        }

        public void QueueDraw()
        {
            mProfitView.Display();
        }
#endif

#if __WINDOWS__
        override public void Show()
        {
            this.Visibility = System.Windows.Visibility.Visible;
        }

        public void Hide()
        {
            this.Visibility = System.Windows.Visibility.Hidden;
        }
        public void QueueDraw()
        {
            canvas.UpdateLayout();
            this.Show();
        }
#endif

        public void ParameterChanged(object sender, EventArgs e)
        {
            QueueDraw();
        }

#if __MACOS__
        public void Display()
        {
            throw new NotImplementedException();
        }

        public ProfitPage(TraderBook traderBook, CGRect frameRect) : base()
        {
            mTraderBook = traderBook;
            mProfitView = new ProfitView(traderBook,frameRect);
            // mDrawingArea.ExposeEvent += ProfitPage_ExposeEvent;
            // ParameterBar = new ParameterBar(parent);
            this.View = mProfitView;
        }
#endif

#if __WINDOWS__
        public ProfitPage (TraderBook parent) : base(parent, PageType.Profit)
        {
            TraderBook = parent;
            this.parent = parent;

            StackPanel sp = new StackPanel() { Margin = new Thickness(0) };
            canvas = new Canvas();
            canvas.Height = 700;
            parameterBar = new ParameterBar(parent);
            sp.Children.Add(canvas);
            sp.Children.Add(parameterBar);
            base.Children.Add(sp);
            parent.OnParameterChanged += ParameterChanged;
        }

        void ProfitPage_ExposeEvent(object sender, EventArgs args)
        {
            // Cairo.Context context = Gdk.CairoHelper.Create(mDrawingArea.GdkWindow);
            double width = canvas.ActualWidth;
            double height = canvas.Height;

            // A white background to start
            canvas.Children.Clear();
            canvas.Background = new SolidColorBrush(Colors.White);

            try
            {
                if (mBars == null || mPositions == null)
                    return;


                int first = mBars.Count;
                int last = 0;
                foreach (Position position in mPositions)
                {
                    if (position.Status == PositionStatus.Closed)
                    {
                        if (position.OpenBar < first)
                            first = position.OpenBar;
                        if (position.CloseBar > last)
                            last = position.CloseBar;
                    }
                }

                double[] worth = new double[last - first + 1];
                for (int bar = first; bar <= last; bar++)
                    worth[bar - first] = 0.0;

                foreach (Position position in mPositions)
                {
                    if (position.Status == PositionStatus.Closed)
                    {
                        for (int bar = position.OpenBar; bar <= last; bar++)
                            worth[bar - first] -= position.OpenPrice * 1.01;
                        for (int bar = position.CloseBar; bar <= last; bar++)
                            worth[bar - first] += position.ClosePrice * 0.99;
                    }
                }

                double max = 0;
                double min = 0;
                for (int bar = first; bar <= last; bar++)
                {
                    if (worth[bar - first] < min)
                        min = worth[bar - first];
                    if (worth[bar - first] > max)
                        max = worth[bar - first];
                    if (mBars.Close[bar] - mBars.Close[first] < min)
                        min = mBars.Close[bar];
                    if (mBars.Close[bar] - mBars.Close[first] > max)
                        max = mBars.Close[bar];
                }

                // context.Scale( last-first, max - min );			
                for (int bar = first; bar <= last; bar++)
                {
                    var line = new Line();
                    if (worth[bar - first] < 0)
                        line.Stroke = new SolidColorBrush(new Color() { R = 255, G = 0, B = 0 });
                    else
                        line.Stroke = new SolidColorBrush(new Color() { R = 0, G = 255, B = 0 });
                    line.StrokeThickness = width / (last - first + 1);
                    line.X1 = (bar - first) * width / (last - first);
                    line.Y1 = height - (worth[bar - first] - min) * height / (max - min);
                    line.X2 = (bar - first) * width / (last - first);
                    line.Y2 = height - (0.0 - min) * height / (max - min);
                    canvas.Children.Add(line);
                }

                for (int bar = first; bar < last; bar++)
                {
                    Line line = new Line();
                    line.Stroke = new SolidColorBrush(Colors.Black);
                    line.X1 = (bar - first) * width / (last - first);
                    line.Y1 = height - (mBars.Close[bar] - mBars.Close[first] - min) * height / (max - min);
                    line.X2 = (bar - first + 1) * width / (last - first);
                    line.Y2 = height - (mBars.Close[bar + 1] - mBars.Close[first] - min) * height / (max - min);
                    canvas.Children.Add(line);
                }
            }
            catch (Exception exception)
            {
                #region debug message						
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(exception, true);
                string stringLineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
                string message = "(" + stringLineNumber + ") " + exception.Message;
                MessageBox.Show(message);
                #endregion
            }
            canvas.UpdateLayout();
        }
#endif
    }
}
