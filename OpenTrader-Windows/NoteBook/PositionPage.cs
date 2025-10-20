using System;
using System.Collections.Generic;
#if __MACOS__
using AppKit;
using Foundation;
using CoreGraphics;
#endif
#if __WINDOWS__
using System.Windows.Controls;
using System.Windows.Data;
#endif
using Accord.Statistics.Testing;

namespace OpenTrader
{
#if __MACOS__
    public class PositionPage : NSTabViewItem, ITraderPage
#endif
#if __WINDOWS__
    public class PositionPage : TraderPage
#endif
    {
        List<Position> mPositions;
#if __MACOS__
        PositionSource mPositionSource;
#endif

#if __MACOS__
        NSTableView mTreeView;
        NSScrollView positionsScrollView;
        NSScrollView statsScrollView;
        NSTextView statsTextView;
        NSSplitView pageView;
#endif
#if __WINDOWS__
        DataGrid mTreeView;
#endif

        public List<Position> Positions
        {
            get { return mPositions; }
            set
            {
                mPositions = value;
#if __MACOS__
                mPositionSource.Positions = mPositions;
                AnalyseStatistics();
                if (mTraderBook.DrawingPaused)
                        return;
                mTreeView.ReloadData();
#endif
#if __WINDOWS__
                if (mTreeView != null)
                    mTreeView.ItemsSource = mPositions;
#endif
            }
        }

        private void AnalyseStatistics()
        {
            if (Positions == null)
                return;
            int n = 0;
            int profits = 0;
            double sumProfit = 0;
            double sumLoss = 0;
            double sum = 0;

            List<double> samples = new List<double>();
            foreach (Position position in Positions)
            {
                if (position.Status == PositionStatus.Closed)
                {
                    sum += (position.ClosePrice - position.OpenPrice) / position.OpenPrice;
                    if (position.ClosePrice > position.OpenPrice)
                    {
                        sumProfit += (position.ClosePrice - position.OpenPrice) / position.OpenPrice;
                        profits += 1;
                    }
                    else
                    {
                        sumLoss += (position.OpenPrice - position.ClosePrice) / position.OpenPrice;
                    }

                    samples.Add((position.ClosePrice - position.OpenPrice) / position.OpenPrice);
                    n++;
                }
            }
            ClearLines();
            WriteLine(profits.ToString() + " profits out of N =" + n.ToString());
            WriteLine(((double)profits * 100.0 / (double)n).ToString() + " trades were profitable.");
            WriteLine("The average trade was " + (sum * 100 / profits).ToString("0:0.00") + "%");
            WriteLine("The average profit was " + (sumProfit * 100 / profits).ToString("0:0.00") + "%");
            WriteLine("The average loss was " + (sumLoss * 100 / (n - profits)).ToString("0:0.00") + "%");

            if (samples.Count < 4)
            {
                WriteLine("Data must contain at least 4 observations for a ShapiroWilk test.");
            }
            else
            { 
                var sw = new ShapiroWilkTest(samples.ToArray());

                double W = sw.Statistic; // should be 0.90050
                double p = sw.PValue;    // should be 0.04209
                bool significant = sw.Significant; // should be true
                WriteLine("This data is " + (significant ? "not" : "") + " normally distributed.");

                var normal = new Accord.Statistics.Distributions.Univariate.NormalDistribution();
                var shapiroWilkTest = new Accord.Statistics.Testing.ShapiroWilkTest(samples.ToArray());
                WriteLine("The ShapiroWilkTest was " + (shapiroWilkTest.Significant ? "" : "not") + " significant");
            }



            var logNormal = new Accord.Statistics.Distributions.Univariate.LognormalDistribution();
            var lognormalTest = new Accord.Statistics.Testing.AndersonDarlingTest(samples.ToArray(), logNormal);
            WriteLine("The LognormalTest was " + (lognormalTest.Significant ? "" : "not") + " significant");

            WriteLine("");
            WriteLine("Analysing successful trades using the binomial distribution");
            double mean = (double)profits / (double)n;
            double variance = ((mean*mean)*profits + (1.0-mean)+(1.0-mean)*(n-profits)) / (n-1);
            double stddev = Math.Sqrt(variance);
            double stderror = 1.96 * stddev / Math.Sqrt((double)n);

            WriteLine("The standard deviation was " + (stddev * 100).ToString() + "%");
            WriteLine("The margin of error was  " + (stderror * 100).ToString() + "%");
        }

        public PageType PageType
        {
            get { return PageType.Position; }
        }

#if __MACOS__
        public void ClearLines()
        {
            int length = statsTextView.String.Length;
            if (length > 0)
            {
                NSRange range = new NSRange(1, statsTextView.String.Length);
                statsTextView.TextStorage.SetString(new NSAttributedString());
            }
        }
#endif
#if __WINDOWS__
        public void ClearLines()
        {
        }
#endif

#if __MACOS__
        public void WriteLine(string text)
        {
            statsTextView.TextStorage.Append(new NSAttributedString(text + "\r\n"));
            NSRange range = new NSRange(statsTextView.String.Length, 0);
            statsTextView.ScrollRangeToVisible(range);
        }
#endif
#if __WINDOWS__
        public void WriteLine(string text)
        {
        }
#endif

#if __MACOS__
        public void Write(string text)
        {
            statsTextView.TextStorage.Append(new NSAttributedString(text));
            NSRange range = new NSRange(statsTextView.String.Length, 0);
            statsTextView.ScrollRangeToVisible(range);
        }
#endif
#if __WINDOWS__
        public void Write(string text)
        {
        }
#endif
        private TraderBook mTraderBook;
        public TraderBook TraderBook {
            get { return mTraderBook;  }
            set { mTraderBook = value; }
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
#endif
#if __WINDOWS__
        override public void Show()
        {
            Visibility = System.Windows.Visibility.Visible;
        }

        public void Hide()
        {
            Visibility = System.Windows.Visibility.Collapsed;
        }
#endif

#if __MACOS__
        public PositionPage(TraderBook traderBook, CGRect frameRect)
        {

            mTraderBook = traderBook;
            CoreGraphics.CGRect pageRect = new CoreGraphics.CGRect(0, 0, frameRect.Width, frameRect.Height);

            // Setup positions view
            pageView = new NSSplitView(pageRect);

            CoreGraphics.CGRect positionsRect = new CoreGraphics.CGRect(0, 0, frameRect.Width, frameRect.Height-250);

            positionsScrollView = new NSScrollView(frameRect);
            NSViewController svController = new NSViewController();
            CoreGraphics.CGRect svRect = new CoreGraphics.CGRect(0, 0, frameRect.Width, frameRect.Height);

            svController.View = positionsScrollView;
            CoreGraphics.CGRect ovRect = new CoreGraphics.CGRect(0, 0, frameRect.Width, frameRect.Height);
            mTreeView = new NSTableView(ovRect);
            positionsScrollView.DocumentView = mTreeView;
            // vcDatasets.View = ov;
            mPositionSource = new PositionSource(mTraderBook);
            mTreeView.DataSource = mPositionSource;
            // tv.Delegate = new ProfileDelegate();

            NSTableColumn profitColumn = new NSTableColumn("Profit");
            profitColumn.HeaderCell.StringValue = "Profit";
            profitColumn.Editable = false;
            profitColumn.MinWidth = 160f;
            mTreeView.AddColumn(profitColumn);

            NSTableColumn typeColumn = new NSTableColumn("Type");
            typeColumn.HeaderCell.StringValue = "Type";
            typeColumn.Editable = false;
            typeColumn.MinWidth = 160f;
            mTreeView.AddColumn(typeColumn);

            NSTableColumn callsColumn = new NSTableColumn("EntryDate");
            callsColumn.HeaderCell.StringValue = "Entry";
            callsColumn.Editable = false;
            callsColumn.MinWidth = 160f;
            mTreeView.AddColumn(callsColumn);


            NSTableColumn entryDateColumn = new NSTableColumn("EntrySignal");
            entryDateColumn.HeaderCell.StringValue = "Signal";
            entryDateColumn.Editable = false;
            entryDateColumn.MinWidth = 160f;
            mTreeView.AddColumn(entryDateColumn);

            NSTableColumn entryPriceColumn = new NSTableColumn("EntryPrice");
            entryPriceColumn.HeaderCell.StringValue = "Price";
            entryPriceColumn.Editable = false;
            entryPriceColumn.MinWidth = 160f;
            mTreeView.AddColumn(entryPriceColumn);

            NSTableColumn exitDateColumn = new NSTableColumn("ExitDate");
            exitDateColumn.HeaderCell.StringValue = "Exit";
            exitDateColumn.Editable = false;
            exitDateColumn.MinWidth = 160f;
            mTreeView.AddColumn(exitDateColumn);


            NSTableColumn exitSignalColumn = new NSTableColumn("ExitSignal");
            exitSignalColumn.HeaderCell.StringValue = "Signal";
            exitSignalColumn.Editable = false;
            exitSignalColumn.MinWidth = 160f;
            mTreeView.AddColumn(exitSignalColumn);

            NSTableColumn exitPriceColumn = new NSTableColumn("ExitPrice");
            exitPriceColumn.HeaderCell.StringValue = "Price";
            exitPriceColumn.Editable = false;
            exitPriceColumn.MinWidth = 160f;
            mTreeView.AddColumn(exitPriceColumn);

            positionsScrollView.DocumentView = mTreeView;
            positionsScrollView.HasHorizontalScroller = true;
            positionsScrollView.HasVerticalScroller = true;

            // Setup stats view
            CGRect statsRect = new CoreGraphics.CGRect(0, frameRect.Height - 250, frameRect.Width, 250);


            statsScrollView = new NSScrollView(statsRect);

            statsTextView = new NSTextView(statsRect);
            statsTextView.Editable = false;
            statsTextView.Selectable = true;
            statsTextView.VerticallyResizable = true;
            statsTextView.HorizontallyResizable = true;
            statsScrollView.DocumentView = statsTextView;

            pageView.AddSubview(positionsScrollView); // sv
            pageView.AddSubview(statsScrollView); // sv
            this.View = pageView;
        }
#endif

#if __WINDOWS__
        public PositionPage (TraderBook Parent) : base( Parent, PageType.Position )
		{
			mTreeView = new DataGrid();

            // Profit
            mTreeView.Columns.Add(new DataGridTextColumn()
            {
                Header = "Profit",
                FontSize = 12,
                Binding = new Binding("Profit"),
                CanUserResize = true
            });
			
			// Long or short
            mTreeView.Columns.Add(new DataGridTextColumn()
            {
                Header = "Type",
                Width = new DataGridLength(200),
                FontSize = 12,
                Binding = new Binding("Type"),
                CanUserResize = true
            });			
			
			// Entry Date
            mTreeView.Columns.Add(new DataGridTextColumn()
            {
                Header = "Entry",
                Width = new DataGridLength(200),
                FontSize = 12,
                Binding = new Binding("OpenDate"),
                CanUserResize = true
            });

			
			// Entry Signal
            mTreeView.Columns.Add(new DataGridTextColumn()
            {
                Header = "Signal",
                Width = new DataGridLength(200),
                FontSize = 12,
                Binding = new Binding("OpenSignal"),
                CanUserResize = true
            });	
			
			// Entry Price
            mTreeView.Columns.Add(new DataGridTextColumn()
            {
                Header = "Price",
                Width = new DataGridLength(200),
                FontSize = 12,
                Binding = new Binding("OpenPrice"),
                CanUserResize = true
            });		
			
			// Exit Date
            mTreeView.Columns.Add(new DataGridTextColumn()
            {
                Header = "Exit",
                Width = new DataGridLength(200),
                FontSize = 12,
                Binding = new Binding("CloseDate"),
                CanUserResize = true
            });		
			
			// Exit Signal
            mTreeView.Columns.Add(new DataGridTextColumn()
            {
                Header = "Signal",
                Width = new DataGridLength(200),
                FontSize = 12,
                Binding = new Binding("CloseSignal"),
                CanUserResize = true
            });			
			
			// Exit Price
            mTreeView.Columns.Add(new DataGridTextColumn()
            {
                Header = "Price",
                Width = new DataGridLength(200),
                FontSize = 12,
                Binding = new Binding("ClosePrice"),
                CanUserResize = true
            });

            mTreeView.ItemsSource = mPositions;
			
			ScrollViewer scrolledwindow = new ScrollViewer();
			scrolledwindow.Content = mTreeView;
		}

#endif

#if __MACOS__
        public void Display()
        {
            pageView.Display();
        }
#endif
    }
}

