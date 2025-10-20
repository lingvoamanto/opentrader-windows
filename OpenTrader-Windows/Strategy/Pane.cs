using System;
using System.Collections.Generic;
#if __APPLEOS__
using CoreGraphics;
#endif
#if __WINDOWS__
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
#endif

namespace OpenTrader
{
    public class Pane
    {
        public int size;

        // temporary variables for storing drawing information for a pane
        public int origin;
        public double min;
        public double max;
        public bool HasRange { get; private set; }
        public double InitialMin { get; private set; }
        public double InitialMax { get; private set; }

        private double overBought = double.NaN;
        public double OverBought { get => overBought; set =>overBought=value; }

        private double overSold = double.NaN;
        public double OverSold{ get => overSold; set => overSold = value; }

#if __APPLEOS__
        public CGRect drawingarea;
        public BaseChartView chart;
#endif


#if __WINDOWS__
        FontFamily fontFamily; FontStyle fontStyle; FontStretch fontStretch;

        public Rect drawingArea;
        public WPFChart chart;

        public Size TextExtents(string text, FontWeight fontWeight, double fontSize)
        {
            FormattedText ft = new FormattedText(text,
                                                 CultureInfo.CurrentCulture,
                                                 FlowDirection.LeftToRight,
                                                 new Typeface(fontFamily, fontStyle, fontWeight, fontStretch),
                                                 fontSize,
                                                 Brushes.Black);
            return new Size(ft.Width, ft.Height);
        }
#endif
        public void SetRange(double overBought, double overSold)
        {
            if( HasRange )
            {
                InitialMin = Math.Min(InitialMin, Math.Min(overBought, overSold));
                InitialMax = Math.Max(InitialMax, Math.Max(overBought, overSold));
            }
            else
            {
                InitialMin = Math.Min(overBought, overSold);
                InitialMax = Math.Max(overBought, overSold);
            }

            HasRange = true;
        }

        public Pane(int size)
        {
            this.size = size;
            DataSeriess = new List<DataSeries>();
            PaneLabels = new List<ColorString>();
        }

        public List<ColorString> PaneLabels;
        public List<DataSeries> DataSeriess;

    }
}
