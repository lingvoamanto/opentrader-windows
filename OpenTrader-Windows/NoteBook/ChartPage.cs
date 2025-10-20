using System;
using System.Collections.Generic;
using Microsoft.CSharp;
using System.IO;
#if __WINDOWS__
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using OpenTrader.Widgets;
#endif
#if __MACOS__
using AppKit;
using CoreGraphics;
using Foundation;
#endif

namespace OpenTrader
{
#if __WINDOWS__
    public class ChartPage : TraderPage
#endif
#if __MACOS__
    public class ChartPage : NSSplitView, ITraderPage, INSSplitViewDelegate
#endif
    {
#if __WINDOWS__
        Controls.ChartControl chartControl;

        public double CandleWidth
        {
            get => chartControl.CandleWidth;
        }

        public Controls.ChartControl ChartControl
        {
            get => chartControl;
        }
#endif

#if __MACOS__
        private ParameterBar mParameterBar;
        ChartView mChartView;
        private DetailsBar mDetailsBar;
        private NSSlider mSlider;
        int mCandleWidth;
        private ParameterBar mParameterbar;

        public ChartPage(IntPtr handle) : base(handle)
        {
        }

        public ParameterBar ParameterBar
        {
            get { return mParameterBar; }
        }
#endif



        public PageType PageType
        {
            get { return OpenTrader.PageType.Chart; }
        }


        public TraderBook mTraderBook;
        public TraderBook TraderBook
        {
            get { return mTraderBook; }
            set { mTraderBook = value;  }
        }

#if __MACOS__
        public void UpdateDetailsBar(int bar)
        {
            mDetailsBar.Update(bar);
        }

        public int CurrentBar
        {
            get { return mSlider.IntValue ; }
            set { mSlider.IntValue = value;  }
        }

        int mBarsToView;
        public int BarsToView
        {
            get { return mBarsToView; }
        }

        public void GotoLastBar()
        {
            CurrentBar = (int) mSlider.MaxValue;
            mChartView.Display();
        }

        public void GotoFirstBar()
        {
            CurrentBar = 0;
            mChartView.Display();
        }

        public void GotoPreviousPage()
        {
            int currentBar = CurrentBar - BarsToView;
            if (currentBar < 0)
                currentBar = 0;
            CurrentBar = currentBar;
            mChartView.Display();
        }

        public void GotoNextPage()
        {
            int currentBar = CurrentBar + BarsToView;
            if (currentBar > mSlider.MaxValue)
                currentBar = (int) mSlider.MaxValue;
            CurrentBar = currentBar;
            mChartView.Display();
        }

        public void SetBarsOnPage(float width)
        {

            if (TraderBook.DataFile == null)
            {
                mSlider.MaxValue = 0;
                mBarsToView = 0;
            }
            else
            {
                mBarsToView = (int)(width / ((float)mCandleWidth * 2.0f));
                if (mBarsToView > TraderBook.DataFile.bars.Count)
                {
                    mBarsToView = TraderBook.DataFile.bars.Count;
                    mSlider.MaxValue = 0;
                }
                else
                {
                    mSlider.MaxValue = TraderBook.DataFile.bars.Count - mBarsToView;
                }
            }
        }

        public void UpdateCandleWidth(float width, int candleWidth)
        {
            mCandleWidth = candleWidth;
            SetBarsOnPage(width);
        }
#endif

        public void DataFileChanged()
        {
#if __APPLEOS__
            SetBarsOnPage((float) mChartView.DrawingArea.Width);
#endif
            double lastBar = TraderBook.DataFile.bars.Count-1;
#if __APPLEOS__
            double firstBar = lastBar+2 - mChartView.DrawingArea.Width / (mCandleWidth * 2);
            CurrentBar = Math.Max(0,(int) firstBar);
            mSlider.MaxValue = Math.Max(0, (int)firstBar);
#endif
#if __WINDOWS__
            chartControl.UpdateScrollbar();
            chartControl.UpdateWebPage();
            chartControl.UpdatePropertiesBar();
#endif
        }

#if __MACOS__
        public void QueueDraw()
        {
            double lastBar = TraderBook.DataFile.bars.Count - 1;
            if( lastBar < mSlider.MaxValue )
            {
                double firstBar = lastBar+2 - (int)(mChartView.DrawingArea.Width) / ((float)mCandleWidth * 2.0f);
                mSlider.IntValue = Math.Max(0, (int)firstBar);
                mSlider.MaxValue = Math.Max(0, (int)firstBar);
            }
            mChartView.Display();

            //this.Display();
        }
#endif
#if __WINDOWS__
        public void QueueDraw()
        {
            chartControl.UpdateScrollbar();
            chartControl.UpdateLayout();
            chartControl.UpdateWebPage();
            chartControl.UpdatePropertiesBar();
            chartControl.WeekControl?.UpdateScrollbar();
            chartControl.WeekControl?.UpdateLayout();
            chartControl.WeekControl?.UpdatePropertiesBar();
            base.UpdateLayout();
            this.Show();
        }

        public void BuildParameters()
        {
            chartControl.BuildParameters();
        }
#endif

        public void ParameterChanged(object sender, EventArgs e)
        {
            QueueDraw();
        }

#if __MACOS__
        public ChartPage(TraderBook traderBook) : base()
        {
            this.TranslatesAutoresizingMaskIntoConstraints = true;

            mTraderBook = traderBook;

            mCandleWidth = 5;

            // Establish the size of everything
            CGRect parentRect = mTraderBook.VisibleRect();
            CGRect chartRect = new CGRect(parentRect.Left+20, parentRect.Top, parentRect.Width-40, parentRect.Height - 250);
            CGRect sliderRect = new CGRect(parentRect.Left, parentRect.Top + parentRect.Height - 200, parentRect.Width, 100);
            CGRect detailsRect = new CGRect(parentRect.Left, parentRect.Top + parentRect.Height - 100, parentRect.Width, 50);
            CGRect parameterRect = new CGRect(parentRect.Left, parentRect.Top + parentRect.Height - 50, parentRect.Width, 50);

            // Alignment codeAlignment = new Alignment((float)0.0, (float)0.5, (float)0.0, (float)0.0);


            // TODO add all this back
            mDetailsBar = new DetailsBar(detailsRect, mTraderBook.DataFile.bars);
            mParameterBar = new ParameterBar(parameterRect, mTraderBook);



            mSlider = new NSSlider(sliderRect);
            mSlider.IntValue = 0;
            mSlider.MinValue = 0;
            mSlider.MaxValue = 0;
            mSlider.SliderType = NSSliderType.Linear;
            mSlider.AltIncrementValue = 100;
            mSlider.Activated += Slider_ValueChanged;

            // remove adjustment from this for the time being
            mChartView = new ChartView(chartRect, mCandleWidth, this);

            this.AddSubview(mChartView);
            this.AddSubview(mSlider);
            this.AddSubview(mDetailsBar);
            this.AddSubview(mParameterBar);
            mChartView.Display();
            this.Display();
        }
#endif
#if __WINDOWS__


 
        public ChartPage( TraderBook traderBook ) : base( traderBook, PageType.Chart )
		{
            // mAdjustment = new Adjustment();
            // mAdjustment.Value = 1;
            // mAdjustment.PageSize = 0; //  200;	
                                      // mVBox = new VBox( false, 0 );

            chartControl = new Controls.ChartControl(ChartType.Day,traderBook);
            this.Children.Add(chartControl);
            chartControl.Canvas.SizeChanged += Canvas_SizeChanged;


            // parameterControls = chartControl.ParameterBar;


			// frameStrategies.Add(mVBox);
			
				
			parent.OnParameterChanged += ParameterChanged;
		}


        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            QueueDraw();
        }




#endif
        void Slider_ValueChanged(object sender, EventArgs e)
        {
#if __MACOS__
            mChartView.Display();
#endif
        }
    }

}

