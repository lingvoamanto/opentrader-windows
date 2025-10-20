using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTrader.Data;
#if __MACOS__
using AppKit;
#endif
namespace OpenTrader
{

    public class TraderScript
    {
        internal TraderBook traderbook;

        public List<Pane> Panes;

        public StrategyParameters StrategyParameters = new StrategyParameters();
        public List<Position> Positions;

        public Annotations Annotations;

        public TraderBook TraderBook
        {
            get { return traderbook; }
            set { traderbook = value; }
        }

        protected ChartType chartType;
        public ChartType ChartType
        {
            get { return chartType; }
        }

        public DataSet DataSet { get => traderbook.mDataFile.DataSet; }

        public Bars bars
        {
            get
            {
                if (traderbook == null)
                    return null;
                if (traderbook.mDataFile == null)
                    return null;
                return chartType == ChartType.Week ? traderbook.mDataFile.weekBars : traderbook.mDataFile.bars;
            }
        }

        public double CandleWidth
        {
            get => chartType == ChartType.Week ? (traderbook.WeekChart == null ? 5 : traderbook.WeekChart.CandleWidth) : traderbook.DayChart.CandleWidth;
        }

        protected DataSeries Open
        {
            get { return bars.Open; }
        }

        protected DataSeries High
        {
            get { return bars.High; }
        }

        protected DataSeries Low
        {
            get { return bars.Low; }
        }

        protected DataSeries Close
        {
            get { return bars.Close; }
        }

        public List<DrawingItem> DrawingItems;

        public Pane VolumePane;
        public Pane PricePane;


        private ProfitPage profitpage
        {
            get
            {
                if (traderbook == null)
                    return null;
                else
                {
                    return traderbook.ProfitPage;
                }
            }
        }


        private EditorPage editorpage
        {
            get
            {
                if (traderbook == null)
                    return null;
                else
                {
                    return traderbook.EditorPage;
                }
            }
        }

        public StrategyParameter CreateParameter(string name, double value, double start, double stop, double step)
        {
            StrategyParameter parameter = new StrategyParameter(StrategyParameters)
            {
                Name = name,
                Value = value,
                Start = start,
                Stop = stop,
                Step = step
            };

            StrategyParameters.Add(parameter);
            return parameter;
        }

        public List<DataSet> DataSets
        {
            get 
            {
#if __MACOS__
                return (NSApplication.SharedApplication.Delegate as AppDelegate).DataSets; 
#endif
#if __WINDOWS__
                return MainWindow.DataSets;
#endif
            }
        }


        virtual public void Execute()
        {

        }

        virtual public double GetFitness()
        {
            throw new NotImplementedException();
        }

        virtual public bool Alert(DataFile dataFile)
        {
            // var patterns = Indicators.Patterns.Pennant3(dataFile.bars.Close.ToArray());
            var patterns = Indicators.Patterns.HeadShouldersTop(dataFile.bars.Close.ToArray());
            foreach (var pattern in patterns)
            {
                return true;
                try
                {
                    if (pattern.bars[^1] == dataFile.bars.Close.Count - 1)
                    {
                        
                        return true;
                    }
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("found " + dataFile.Description);
                }
            }

            return false;
            // throw new NotImplementedException();
        }

        void PlotPrices(Pane _pane, Bars bars)
        {
            DrawingItem item = new DrawingItem()
            {
                drawingmethod = DrawingMethod.PlotPrices,
                pane = _pane,
                parameters = new object[] { (object)bars }
            };
            DrawingItems.Add(item);
            _pane.DataSeriess.Add(bars.Open);
            _pane.DataSeriess.Add(bars.High);
            _pane.DataSeriess.Add(bars.Low);
            _pane.DataSeriess.Add(bars.Close);
        }


        public Pane CreatePane(int size)
        {
            Pane pane = new Pane(size);
            Panes.Add(pane);
            return pane;
        }

        protected Position LastActivePosition
        {
            get
            {
                Position result = null;
                foreach (Position position in Positions)
                {
                    if (position.Status == PositionStatus.Active)
                        result = position;
                }
                return result;
            }
        }

        public List<Position> AllPositions
        {
            get { return Positions; }
        }

        protected Position BuyAtMarket(int bar)
        {
            Position position = new Position(bar, bars.Open[bar], PositionType.LongPosition);
            Positions.Add(position);
            Annotations[bar].position.Add(position);
            traderbook.OnSignalChanged(position);
            return position;
        }

        protected Position BuyAtMarket(int bar, string Signal)
        {
            Position position = new Position(bar, bars.Open[bar], PositionType.LongPosition);
            position.OpenSignal = Signal;
            Positions.Add(position);
            Annotations[bar].position.Add(position);
            traderbook.OnSignalChanged(position);
            return position;
        }

        protected void SellAtMarket(int bar, Position position)
        {
            if (position != null)
            {
                position.Close(bar, bars.Open[bar]);
                Annotations[bar].position.Add(position);
            }
        }

        protected void SellAtMarket(int bar, Position position, string Signal)
        {
            if (position != null)
            {
                position.Close(bar, bars.Open[bar]);
                position.CloseSignal = Signal;
                Annotations[bar].position.Add(position);
            }
        }

        protected void SellAtMarket(int bar, List<Position> positions)
        {
            foreach (Position position in positions)
            {
                if (position.Status == PositionStatus.Active)
                {
                    SellAtMarket(bar, position);
                }
            }
        }


        protected void SellAtMarket(int bar, List<Position> positions, string Signal)
        {
            foreach (Position position in positions)
            {
                if (position.Status == PositionStatus.Active)
                {
                    SellAtMarket(bar, position);
                    position.CloseSignal = Signal;
                    Annotations[bar].position.Add(position);
                }
            }
        }

        protected Position BuyAtClose(int bar)
        {
            Position position = new Position(bar, bars.Close[bar], PositionType.LongPosition);
            Positions.Add(position);
            Annotations[bar].position.Add(position);
            traderbook.OnSignalChanged(position);
            return position;
        }

        protected Position BuyAtClose(int bar, string signal)
        {
            Position position = new Position(bar, bars.Close[bar], PositionType.LongPosition);
            position.OpenSignal = signal;
            Positions.Add(position);
            Annotations[bar].position.Add(position);
            traderbook.OnSignalChanged(position);
            return position;
        }

        protected void SellAtClose(int bar, Position position)
        {
            position.Close(bar, bars.Close[bar]);
            Annotations[bar].position.Add(position);
            traderbook.OnSignalChanged(position);
        }

        protected void SellAtClose(int bar, Position position, string signal)
        {
            position.Close(bar, bars.Close[bar]);
            position.CloseSignal = signal;
            Annotations[bar].position.Add(position);
        }

        protected void SellAtClose(int bar, List<Position> positions)
        {
            foreach (Position position in positions)
            {
                if (position.Status == PositionStatus.Active)
                    SellAtClose(bar, position);
            }
        }

        protected Position ShortAtMarket(int bar)
        {
            Position position = new Position(bar, bars.Open[bar], PositionType.ShortPosition);
            Positions.Add(position);
            Annotations[bar].position.Add(position);
            traderbook.OnSignalChanged(position);
            return position;
        }

        protected void CoverAtMarket(int bar, Position position)
        {
            position.Close(bar, bars.Open[bar]);
            Annotations[bar].position.Add(position);
        }

        protected void CoverAtMarket(int bar, List<Position> positions)
        {
            foreach (Position position in positions)
            {
                if (position.Status == PositionStatus.Active)
                    CoverAtMarket(bar, position);
            }
        }

        protected Position ShortAtClose(int bar)
        {
            Position position = new Position(bar, bars.Close[bar], PositionType.ShortPosition);
            Positions.Add(position);
            Annotations[bar].position.Add(position);
            traderbook.OnSignalChanged(position);
            return position;
        }

        protected Position ShortAtClose(int bar, string signal)
        {
            Position position = new Position(bar, bars.Close[bar], PositionType.ShortPosition);
            position.OpenSignal = signal;
            Positions.Add(position);
            Annotations[bar].position.Add(position);
            traderbook.OnSignalChanged(position);
            return position;
        }

        protected void CoverAtClose(int bar, Position position, string signal)
        {
            position.Close(bar, bars.Close[bar]);
            position.CloseSignal = signal;
            Annotations[bar].position.Add(position);
        }

        protected void CoverAtClose(int bar, Position position)
        {
            position.Close(bar, bars.Close[bar]);
            Annotations[bar].position.Add(position);
        }

        protected void CoverAtClose(int bar, List<Position> positions)
        {
            foreach (Position position in positions)
            {
                if (position.Status == PositionStatus.Active)
                    CoverAtClose(bar, position);
            }
        }


        public void PlotSeries(Pane _pane, DataSeries dataseries, System.Drawing.Color color, LineStyle linestyle, int thickness)
        {
            DrawingItem item = new DrawingItem()
            {
                drawingmethod = DrawingMethod.PlotSeries,
                pane = _pane,
                parameters = new object[] { (object)dataseries, (object)color, (object)linestyle, (object)thickness }
            };
            DrawingItems.Add(item);
            _pane.DataSeriess.Add(dataseries);
        }


        public void PlotSeriesFillBand(Pane _pane, DataSeries dataseries1, DataSeries dataseries2, System.Drawing.Color linecolor, System.Drawing.Color bandcolor, LineStyle linestyle, int thickness)
        {
            DrawingItem item = new DrawingItem()
            {
                drawingmethod = DrawingMethod.PlotSeriesFillBand,
                pane = _pane,
                parameters = new object[] { (object)dataseries1, (object)dataseries2, (object)linecolor, (object)bandcolor, (object)linestyle, (object)thickness }
            };
            DrawingItems.Add(item);
            _pane.DataSeriess.Add(dataseries1);
            _pane.DataSeriess.Add(dataseries2);
        }

        public void PlotSeriesOscillator(Pane _pane, DataSeries source, double overbought, double oversold, System.Drawing.Color overboughtColor, System.Drawing.Color oversoldColor, System.Drawing.Color color, LineStyle style, int width)
        {
            DrawingItem item = new DrawingItem()
            {
                drawingmethod = DrawingMethod.PlotSeriesOscillator,
                pane = _pane,
                parameters = new object[] { (object)source, (object)overbought, (object)oversold, (object)overboughtColor, (object)oversoldColor, (object)color, (object)style, (object)width }
            };
            DrawingItems.Add(item);
            _pane.DataSeriess.Add(source);
            _pane.SetRange(overbought,oversold);
        }

        public void PlotPattern(int[] source, System.Drawing.Color color, LineStyle style, int width)
        {
            DrawingItem item = new DrawingItem()
            {
                drawingmethod = DrawingMethod.PlotPattern,
                pane = PricePane,
                parameters = new object[] { source,  color, style, width }
            };
            DrawingItems.Add(item);
        }

        public void DrawLine(Pane _pane, int bar1, double value1, int bar2, double value2, Color color, LineStyle style, int width)
        {
            DrawingItem item = new DrawingItem()
            {
                drawingmethod = DrawingMethod.DrawLine,
                pane = _pane,
                parameters = new object[] { (object)bar1, (object)value1, (object)bar2, (object)value2, (object)color, (object)style, (object)width }
            };
            DrawingItems.Add(item);
        }


        public void DrawHorzLine(Pane _pane, double val, System.Drawing.Color color, LineStyle linestyle, int thickness)
        {
            DrawingItem item = new DrawingItem()
            {
                drawingmethod = DrawingMethod.DrawHorzLine,
                pane = _pane,
                parameters = new object[] { (object)val, (object)color, (object)linestyle, (object)thickness }
            };
            DrawingItems.Add(item);
        }

        public void DrawPolygon(Pane _pane, Color color, Color fillColor, LineStyle style, int width, bool behindBars, params double[] coords)
        {
            DrawingItem item = new DrawingItem()
            {
                drawingmethod = DrawingMethod.DrawPolygon,
                pane = _pane,
                parameters = new object[] { (object)color, (object)fillColor, (object)style, (object)width, (object)behindBars, (object)coords },
                behind = behindBars
            };
            DrawingItems.Add(item);
        }

        public void SetBackgroundColor(int bar, System.Drawing.Color color)
        {
            Annotations[bar].backgroundcolor = color;
        }

        public void DrawCircle(int bar, double price, System.Drawing.Color color)
        {
            Annotations[bar].shapes.Add(new ShapeDetail(Shape.Circle,price,color));
        }

        protected bool CrossOver(int bar, DataSeries dataseries, double threshold)
        {
            if (bar > 0 && bar < dataseries.Count)
                return dataseries[bar - 1] < threshold && dataseries[bar] > threshold;
            else
                return false;
        }

        protected bool CrossUnder(int bar, DataSeries dataseries, double threshold)
        {
            if (bar > 0 && bar < dataseries.Count)
                return dataseries[bar - 1] > threshold && dataseries[bar] < threshold;
            else
                return false;
        }

        protected bool CrossOver(int bar, DataSeries dataseries, DataSeries threshold)
        {
            if (bar > 0 && bar < dataseries.Count)
                return dataseries[bar - 1] < threshold[bar - 1] && dataseries[bar] > threshold[bar];
            else
                return false;
        }

        protected bool CrossUnder(int bar, DataSeries dataseries, DataSeries threshold)
        {
            if (bar > 0 && bar < dataseries.Count)
                return dataseries[bar - 1] > threshold[bar - 1] && dataseries[bar] < threshold[bar];
            else
                return false;
        }

        public void DrawLabel(Pane pane, string label, System.Drawing.Color color)
        {
            ColorString panelabel = new ColorString()
            {
                Text = label,
                Color = color
            };
            pane.PaneLabels.Add(panelabel);
        }

        public void AnnotateBar(string text, int bar, bool above, System.Drawing.Color fontcolor, System.Drawing.Color background, Font? font=null)
        {
            ColorString colorstring = new ColorString()
            {
                Text = text,
                Above = above,
                Font = font,
                Color = fontcolor,
                Background = background
            };
            Annotations[bar].colorstring.Add(colorstring);
        }

        public TraderScript()
        {
        }

        public void Run(ChartType chartType=ChartType.Day)
        {
            this.chartType = chartType;
            if (bars == null)
                return;

            bars.ClearCache();

            DrawingItems = new List<DrawingItem>();
            Panes = new List<Pane>();
            Positions = new List<Position>();
            Annotations = new Annotations(bars.Count);

            VolumePane = CreatePane(10);
            PricePane = CreatePane(90);
            PlotSeries(VolumePane, bars.Volume, System.Drawing.Color.Black, LineStyle.Histogram, 0);
            PlotPrices(PricePane, bars);

            try
            {
                Execute();
            }
            catch (Exception debugException)
            {
                var stack = new System.Diagnostics.StackTrace(debugException, true);
                string message = debugException.Message; 
                if (stack != null)
                {
                    var frame = stack.GetFrame(2);
                    if (frame != null)
                    {
                        message = $"{frame.GetFileLineNumber()} {frame.GetFileName()} {message}";
                    }
                } 

                this.traderbook.EditorPage.WriteLine(message + " when trying to execute");
            }

            if (Positions.Count > 0)
            {
                traderbook.PositionPage.Positions = Positions;
                traderbook.ProfitPage.Bars = traderbook.mDataFile.bars;
                traderbook.ProfitPage.Positions = Positions;
                traderbook.PositionPage.Show();
                traderbook.ProfitPage.Show();
            }
            else
            {
                traderbook.PositionPage.Positions = null;
                traderbook.ProfitPage.Positions = null;
                traderbook.PositionPage.Hide();
                traderbook.ProfitPage.Hide();
            }

            profitpage.QueueDraw();
        }
    }
}

