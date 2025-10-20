using OpenTrader.Data;
using OpenTrader.Windows;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenTrader
{
    public class DayChart : DirectXChart
    {
        static FontFamily systemFontFamily = new FontFamily(System.Drawing.SystemFonts.DefaultFont.Name);
        const double lineStrokeThickness = 1.5;

        ContextMenu lineMenu = new ContextMenu();
        ContextMenu textMenu = new ContextMenu();
        ContextMenu generalMenu = new ContextMenu();
        ContextMenu measureMenu = new ContextMenu();
        static BitmapImage selectionImage = SelectionImage();
        static BitmapImage moveImage = MoveImage();
        static BitmapImage deleteImage = DeleteImage();
        static BitmapImage measureImage = MeasureImage();
        static Brush dividendBrush = new SolidColorBrush(Color.FromArgb(192,192,192,192));

        int barInCanvas;
        double priceInCanvas;

        enum EditMode { None, DrawLine, DrawAnnotation, Stretch, MoveLine, MoveText }

        EditMode editMode;
        Marker editMarker;

        enum AnnotationType { LinearTrend, QuadraticTrend, MeasureRule, Text }
        AnnotationType annotationType;

        short pointCount;

        Data.TrendLine trendLine;

        Controls.ChartControl chartControl;

        #region Image Initialisation
        static BitmapImage SelectionImage()
        {
            var selectionImage = new System.Windows.Media.Imaging.BitmapImage();
            selectionImage.BeginInit();
            selectionImage.UriSource = new Uri("pack://application:,,,/OpenTrader;component/images/Selection.png");
            selectionImage.EndInit();

            return selectionImage;
        }

        static BitmapImage MoveImage()
        {
            var moveImage = new System.Windows.Media.Imaging.BitmapImage();
            moveImage.BeginInit();
            moveImage.UriSource = new Uri("pack://application:,,,/OpenTrader;component/images/Move.png");
            moveImage.EndInit();

            return moveImage;
        }

        static BitmapImage DeleteImage()
        {
            var deleteImage = new System.Windows.Media.Imaging.BitmapImage();
            deleteImage.BeginInit();
            deleteImage.UriSource = new Uri("pack://application:,,,/OpenTrader;component/images/Delete.png");
            deleteImage.EndInit();

            return deleteImage;
        }

        static BitmapImage MeasureImage()
        {
            var measureImage = new System.Windows.Media.Imaging.BitmapImage();
            measureImage.BeginInit();
            measureImage.UriSource = new Uri("pack://application:,,,/OpenTrader;component/images/Measure.png");
            measureImage.EndInit();

            return measureImage;
        }
        #endregion Images 

        public DayChart(TraderBook traderBook, Controls.ChartControl chartControl)  : base(traderBook,chartControl)
        {
            this.chartControl = chartControl;

            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseUp += Canvas_MouseUp;

            // Initialise menus
            var move = new MenuItem() { Header = "Move" };
            move.Click += Move_Click;
            move.Icon = new Image() {  Source = moveImage };
            lineMenu.Items.Add(move);
            var stretch = new MenuItem() { Header = "Stretch" };
            stretch.Click += Stretch_Click; 
            stretch.Icon = new Image() { Source = selectionImage };
            lineMenu.Items.Add(stretch);
            var delete = new MenuItem() { Header = "Delete" };
            delete.Icon = new Image() { Source = deleteImage };
            delete.Click += Delete_Click;
            lineMenu.Items.Add(delete);

            var result = new MenuItem() { Header = "Result" };
            result.Click += Result_Click;
            result.Icon = new Image() { Source = measureImage };
            measureMenu.Items.Add(result);
            move = new MenuItem() { Header = "Move" };
            move.Click += Move_Click;
            move.Icon = new Image() { Source = moveImage };
            measureMenu.Items.Add(move);
            stretch = new MenuItem() { Header = "Stretch" };
            stretch.Click += Stretch_Click;
            stretch.Icon = new Image() { Source = selectionImage };
            measureMenu.Items.Add(stretch);
            delete = new MenuItem() { Header = "Delete" };
            delete.Icon = new Image() { Source = deleteImage };
            delete.Click += Delete_Click;
            measureMenu.Items.Add(delete);


            move = new MenuItem() { Header = "Move" };
            move.Click += Move_Click;
            textMenu.Items.Add(move);
            delete = new MenuItem() { Header = "Delete" };
            delete.Click += Delete_Click;
            textMenu.Items.Add(delete);

            // General context menu
            var update = new MenuItem() { Header = "Update" };
            update.Click += Update_Click;
            generalMenu.Items.Add(update);
            var googleNews = new MenuItem() { Header = "News" };
            googleNews.Click += GoogleNews_Click;
            generalMenu.Items.Add(googleNews);
            var trades = new MenuItem() { Header = "Trades" };
            trades.Click += Trades_Click;
            generalMenu.Items.Add(trades);
            var dividends = new MenuItem() { Header = "Dividends" };
            dividends.Click += Dividends_Click;
            generalMenu.Items.Add(dividends);
            var journal = new MenuItem() { Header = "Journal" };
            journal.Click += Journal_Click;
            generalMenu.Items.Add(journal);
            var properties = new MenuItem() { Header = "Properties" };
            properties.Click += Properties_Click;
            generalMenu.Items.Add(properties);
            var removeTrendLine = new MenuItem() { Header = "Remove Line" };
            removeTrendLine.Click += RemoveTrendLine_Click;
            generalMenu.Items.Add(removeTrendLine);
            canvas.ContextMenu = generalMenu;
        }



        private void RemoveTrendLine_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.dataFile != null)
            {
                var pw = new Windows.DataFileWindow(MainWindow.dataFile);
                // pw.Left = Left + (ActualWidth - pw.Width) / 2;
                // pw.Top = Top + 78;
                pw.ShowDialog();
            }
        }

        private void Journal_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.journalWindow == null)
            {
                MainWindow.journalWindow = new Windows.JournalWindow();
                MainWindow.journalWindow.DataFile = MainWindow.dataFile;
                MainWindow.journalWindow.Show();
            }
            else
            {
                MainWindow.journalWindow.Close();
                MainWindow.journalWindow = null;
            }
        }

        private void Dividends_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.dividendsWindow == null)
            {
                MainWindow.dividendsWindow = new Windows.DivendsWindow();
                MainWindow.dividendsWindow.DataFile = MainWindow.dataFile;
                MainWindow.dividendsWindow.Show();
            }
            else
            {
                MainWindow.dividendsWindow.Close();
                MainWindow.dividendsWindow = null;
            }
        }

        private void Trades_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.tradeWindow == null)
            {
                MainWindow.tradeWindow = new Windows.TradeWindow();
                MainWindow.tradeWindow.DataFile = MainWindow.dataFile;
                MainWindow.tradeWindow.Show();
            }
            else
            {
                MainWindow.tradeWindow.Close();
                MainWindow.tradeWindow = null;
            }
        }

        private void GoogleNews_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.newsWindow == null)
            {
                MainWindow.newsWindow = new NewsWindow(MainWindow.dataFile);
                MainWindow.newsWindow.Show();
            }
            else
            {
                MainWindow.newsWindow.Close();
                MainWindow.newsWindow = null;
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public double YToValue(Pane pane, double y)
        {
            double val;
            double y1 = actualHeight - y;
            double range = pane.max - pane.min;
            return (y1 - pane.origin - 0.5) * range / (pane.size * (actualHeight-bottomBarHeight) / totalSize - 1.0) + pane.min;
        }



        public DateTime XToDateTime(double x)
        {
            double bar = XToBar(x);
            return BarToDate(bar);
        }

        public DateTime BarToDate(double bar)
        {
            return traderBook.bars.Date[(int)bar];
        }

        private void StartTrendLine(Data.TrendKind trendKind)
        {
            trendLine = new Data.TrendLine()
            {
                StartDate = BarToDate(barInCanvas),
                StartPrice = trendKind == Data.TrendKind.Measure ? traderBook.bars.High[barInCanvas] : priceInCanvas,
                TrendKind = trendKind,
                YahooCode = traderBook.mDataFile.YahooCode
            };
        }

        private void MidTrendLine()
        {
            trendLine.MidDate = BarToDate(barInCanvas);
            trendLine.MidPrice = trendLine.TrendKind == Data.TrendKind.Measure ? traderBook.bars.Low[barInCanvas] : priceInCanvas;
        }

        private void EndTrendLine()
        {
            trendLine.EndDate = BarToDate(barInCanvas);
            trendLine.EndPrice = trendLine.TrendKind == Data.TrendKind.Measure ? traderBook.bars.Low[barInCanvas] : priceInCanvas;
            trendLine.Save();
            traderBook.TrendLines.Add(trendLine);
        }

        void CreateTrendLine(Point point)
        {
            switch(pointCount)
            {
                case 0:
                    switch(annotationType)
                    {
                        case AnnotationType.QuadraticTrend:
                            StartTrendLine(Data.TrendKind.Quadratic);
                            pointCount++;
                            break;
                        case AnnotationType.MeasureRule:
                            StartTrendLine(Data.TrendKind.Measure);
                            pointCount++;
                            break;
                        case AnnotationType.LinearTrend:
                            StartTrendLine(Data.TrendKind.Linear);
                            pointCount++;
                            break;
                        default:
                            editMode = EditMode.None;
                            pointCount = 0;
                            break;
                    }
                    break;
                case 1:
                    if (annotationType == AnnotationType.QuadraticTrend)
                    {
                        MidTrendLine();
                        pointCount++;
                    }
                    if (annotationType == AnnotationType.MeasureRule)
                    {
                        MidTrendLine();
                        pointCount++;
                    }
                    if (annotationType == AnnotationType.LinearTrend)
                    {
                        EndTrendLine();
                        editMode = EditMode.None;
                        pointCount = 0;
                        DrawLinearTrendLine(trendLine);
                    }
                    break;
                case 2:
                    if (annotationType == AnnotationType.QuadraticTrend)
                    {
                        EndTrendLine();
                        pointCount = 0;
                        editMode = EditMode.None;
                        DrawQuadraticTrendLine(trendLine);
                    }
                    if (annotationType == AnnotationType.MeasureRule)
                    {
                        EndTrendLine();
                        pointCount = 0;
                        editMode = EditMode.None;
                        DrawMeasureRule(trendLine);
                        ShowMeasurePopup(trendLine);
                    }
                    break;
                default:
                    break;
            }
        }

        void CreateText(Point point)
        {
            var annotation = new Data.Annotation();
            annotation.Date = BarToDate(barInCanvas);
            annotation.Price = priceInCanvas;
            annotation.Text = "            ";
            annotation.YahooCode = traderBook.DataFile.YahooCode;
            traderBook.Annotations.Add(annotation);
            DrawScriptAnnotation(annotation,barInCanvas);
        }

        public void QuadraticButton_Click(object o, RoutedEventArgs e)
        {
            annotationType = AnnotationType.QuadraticTrend;
            editMode = EditMode.DrawLine;
            pointCount = 0;
        }

        public void LineButton_Click(object o, RoutedEventArgs e)
        {
            annotationType = AnnotationType.LinearTrend;
            editMode = EditMode.DrawLine;
            pointCount = 0;
        }

        public void TextButton_Click(object o, RoutedEventArgs e)
        {
            annotationType = AnnotationType.Text;
            editMode = EditMode.DrawAnnotation;
            pointCount = 0;
        }

        public void MeasureButton_Click(object sender, RoutedEventArgs e)
        {
            annotationType = AnnotationType.MeasureRule;
            editMode = EditMode.DrawLine;
            pointCount = 0;
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            chartControl.MeasurePopup.IsOpen = false;

            if( editMode == EditMode.DrawLine )
            {
                switch( annotationType )
                {
                    case AnnotationType.LinearTrend:
                    case AnnotationType.QuadraticTrend:
                    case AnnotationType.MeasureRule:
                        CreateTrendLine(e.MouseDevice.GetPosition(canvas));
                        break;
                    default:
                        break;
                }
                return;
            }
            if( editMode == EditMode.DrawAnnotation )
            {
                CreateText(e.MouseDevice.GetPosition(canvas));
                return;
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // canvas.ReleaseMouseCapture();
            if (editMarker != null)
            {
                var trendLine = editMarker.TrendLine;

                if (editMarker.Line is Line line)
                {

                    trendLine.StartDate = XToDateTime(line.X1);
                    trendLine.EndDate = XToDateTime(line.X2);
                    trendLine.StartPrice = YToPrice(line.Y1);
                    trendLine.EndPrice = YToPrice(line.Y2);
                    trendLine.Save();

                    // Align them to the bar
                    line.X1 = DateTimeToX(trendLine.StartDate);
                    line.X2 = DateTimeToX(trendLine.EndDate);

                    InitialiseStretch(line);
                }
                else if (editMarker.Path is Path path)
                {
                    var pathGeometry = path.Data as PathGeometry;
                    var figure = pathGeometry.Figures[0];
                    var segment = figure.Segments[0] as QuadraticBezierSegment;

                    trendLine.StartDate = XToDateTime(figure.StartPoint.X);
                    trendLine.MidDate = XToDateTime(segment.Point1.X);
                    trendLine.EndDate = XToDateTime(segment.Point2.X);
                    trendLine.StartPrice = YToPrice(figure.StartPoint.Y);
                    trendLine.MidPrice = YToPrice(segment.Point1.Y);
                    trendLine.EndPrice = YToPrice(segment.Point2.Y);

                    figure.StartPoint = new Point(DateTimeToX(trendLine.StartDate), figure.StartPoint.Y);
                    segment.Point1 = new Point(DateTimeToX(trendLine.MidDate), segment.Point1.Y);
                    segment.Point2 = new Point(DateTimeToX(trendLine.EndDate), segment.Point2.Y);

                    InitialiseStretch(path);
                }
            }
            editMarker = null;
        }



        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (traderBook.DataFile == null)
                return;

            UpdateBarAndPriceInView(e.MouseDevice.GetPosition(canvas));

            if (editMarker == null)
                return;

            if (editMode == EditMode.Stretch )
            {
                var mouse = e.MouseDevice;
                var position = mouse.GetPosition(canvas);
                Canvas.SetLeft(editMarker.Image, position.X - 7.5);
                Canvas.SetTop(editMarker.Image, position.Y - 7.5);
                switch (editMarker.MarkerType)
                {
                    case MarkerType.Start:
                        if (editMarker.TrendLine.TrendKind == Data.TrendKind.Linear)
                        {
                            editMarker.Line.X1 = position.X;
                            editMarker.Line.Y1 = position.Y;
                        }
                        else if (editMarker.TrendLine.TrendKind == Data.TrendKind.Quadratic)
                        {
                            var pathGeometry = editMarker.Path.Data as PathGeometry;
                            var figure = pathGeometry.Figures[0];
                            figure.StartPoint = position;
                        }
                        break;
                    case MarkerType.Mid:
                        if (editMarker.TrendLine.TrendKind == Data.TrendKind.Quadratic)
                        {
                            var pathGeometry = editMarker.Path.Data as PathGeometry;
                            var figure = pathGeometry.Figures[0];
                            var segment = figure.Segments[0] as QuadraticBezierSegment;
                            segment.Point1 = position;
                        }
                        break;
                    case MarkerType.End:
                        if (editMarker.TrendLine.TrendKind == Data.TrendKind.Linear)
                        {
                            editMarker.Line.X2 = position.X;
                            editMarker.Line.Y2 = position.Y;
                        }
                        else if (editMarker.TrendLine.TrendKind == Data.TrendKind.Quadratic)
                        {
                            var pathGeometry = editMarker.Path.Data as PathGeometry;
                            var figure = pathGeometry.Figures[0];
                            var segment = figure.Segments[0] as QuadraticBezierSegment;
                            segment.Point2 = position;
                        }
                        break;
                    case MarkerType.Move:
                        break;
                }
            }

            if( editMode == EditMode.MoveLine )
            {
                var mouse = e.MouseDevice;
                var position = mouse.GetPosition(canvas);

                var deltaX = position.X - Canvas.GetLeft(editMarker.Image) + 7.5;
                var deltaY = position.Y - Canvas.GetLeft(editMarker.Image) + 7.5;

                if (editMarker.TrendLine.TrendKind == Data.TrendKind.Linear)
                {
                    editMarker.Line.X1 += deltaX;
                    editMarker.Line.Y1 += deltaY;
                }

                if (editMarker.TrendLine.TrendKind == Data.TrendKind.Quadratic)
                {
                    var pathGeometry = editMarker.Path.Data as PathGeometry;
                    var figure = pathGeometry.Figures[0];
                    figure.StartPoint = new Point(figure.StartPoint.X + deltaX, figure.StartPoint.Y + deltaY);
                    var segment = figure.Segments[0] as QuadraticBezierSegment;
                    segment.Point1 = new Point(segment.Point1.X + deltaX, segment.Point1.Y + deltaY);
                    segment.Point2 = new Point(segment.Point2.X + deltaX, segment.Point2.Y + deltaY);
                }

                Canvas.SetLeft(editMarker.Image, position.X - 7.5);
                Canvas.SetTop(editMarker.Image, position.Y - 7.5);
            }

            if (editMode == EditMode.MoveText)
            {
                var mouse = e.MouseDevice;
                var position = mouse.GetPosition(canvas);

                var deltaX = position.X - Canvas.GetLeft(editMarker.Image) + 7.5;
                var deltaY = position.Y - Canvas.GetLeft(editMarker.Image) + 7.5;

                Canvas.SetLeft(editMarker.TextBox, position.X - editMarker.TextBox.Width / 2);
                Canvas.SetBottom(editMarker.TextBox, position.Y - editMarker.TextBox.Height / 2);

                Canvas.SetLeft(editMarker.Image, position.X - 7.5);
                Canvas.SetTop(editMarker.Image, position.Y - 7.5);
            }
        }

        private void UpdateBarAndPriceInView(Point point)
        {
            double i = XToBar(point.X);
            int count = traderBook.mDataFile.bars.Count;
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
                        traderBook.bars.Date[barInCanvas],
                        traderBook.bars.Open[barInCanvas],
                        traderBook.bars.High[barInCanvas],
                        traderBook.bars.Low[barInCanvas],
                        traderBook.bars.Close[barInCanvas],
                        traderBook.bars.Volume[barInCanvas]
                        );
                }
            }

            Pane pricePane = traderBook.TraderScript.PricePane;
            double y = point.Y;
            priceInCanvas = YToPrice(point.Y); 
            
            if( canvas.Tag is Controls.ChartControl chartCtrl)
            {
                chartCtrl.UpdatePrice(priceInCanvas);
            }
            
        }

        private void ShowMeasurePopup(Data.TrendLine trendLine)
        {
            var measureControl = chartControl.MeasurePopup.FindName("MeasureControl") as Controls.MeasureControl;
            if (measureControl != null)
            {
                measureControl.Start.Content = trendLine.StartPrice.ToString("0.000");
                measureControl.Minor.Content = trendLine.MidPrice.ToString("0.000");
                measureControl.End.Content = trendLine.EndPrice.ToString("0.000");

                var target = trendLine.EndPrice + trendLine.StartPrice - trendLine.MidPrice;
                measureControl.Target.Content = target.ToString("0.000");
                var endBar = DateTimeToBar(trendLine.EndDate);
                var move = 100 * (target - traderBook.bars.Close[endBar]) / traderBook.bars.Close[endBar];
                measureControl.Target.Content = target.ToString("0.00") + "%";
                chartControl.MeasurePopup.IsOpen = true;
            }
        }

        private void Result_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var uiElement = ((ContextMenu)menuItem.Parent).PlacementTarget;
                if (uiElement is Path path)
                {
                    if (path.Tag is Data.TrendLine trendLine)
                    {
                        ShowMeasurePopup(trendLine);
                    }
                    return;
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                var uiElement = ((ContextMenu)menuItem.Parent).PlacementTarget;

                if (uiElement is Line line)
                {
                    if (line.Tag is Data.TrendLine trendLine)
                    {
                        trendLine.Remove();
                        canvas.Children.Remove(line);
                    }
                    return;
                }

                // Remove quadratic and measure
                if (uiElement is Path path)
                {
                    if (path.Tag is Data.TrendLine trendLine)
                    {
                        trendLine.Remove();
                        canvas.Children.Remove(path);
                    }
                    return;
                }

                if (uiElement is TextBox textBox)
                {
                    if (textBox.Tag is Data.Annotation annotation)
                    {
                        traderBook.Annotations.Remove(annotation);
                        annotation.Remove();
                        canvas.Children.Remove(textBox);
                    }
                    return;
                }
            }
        }

        private void ClearImages()
        {
            var images = canvas.Children.OfType<Image>().ToList();
            foreach (var image in images)
            {
                canvas.Children.Remove(image);
            }
        }

        private void InitialiseMove(TextBox textBox)
        {
            ClearImages();
            editMode = EditMode.MoveLine;
            var annotation = textBox.Tag as Data.Annotation;

            var moveMarker = new Marker()
            {
                Annotation = annotation,
                TextBox = textBox,
                MarkerType = MarkerType.Move
            };
            var x = Canvas.GetRight(textBox);
            var y = Canvas.GetTop(textBox);
            CreateMarkerImage(moveMarker, x - textBox.Width / 2 - 7.5, y - textBox.Height / 2 + 7.5);
        }


        private void InitialiseMove(Line line)
        {
            ClearImages();
            editMode = EditMode.MoveLine;
            var trendLine = line.Tag as Data.TrendLine;

            var moveMarker = new Marker()
            {
                TrendLine = trendLine,
                Line = line,
                MarkerType = MarkerType.Move
            };
            CreateMarkerImage(moveMarker, (line.X1+line.Y1)/2, (line.Y1+line.Y2)/2);
        }

        private void InitialiseMove(Path path)
        {
            ClearImages();
            editMode = EditMode.MoveLine;
            var trendLine = path.Tag as Data.TrendLine;

            var moveMarker = new Marker()
            {
                TrendLine = trendLine,
                Path = path,
                MarkerType = MarkerType.Move
            };

            var pathGeometry = path.Data as PathGeometry;
            var figure = pathGeometry.Figures[0];

            if (trendLine.TrendKind == Data.TrendKind.Quadratic)
            {
                var segment = figure.Segments[0] as QuadraticBezierSegment;
                CreateMarkerImage(moveMarker, segment.Point1.X, segment.Point1.Y);
            }

            if (trendLine.TrendKind == Data.TrendKind.Quadratic)
            {
                var segment1 = figure.Segments[0]  as LineSegment;
                CreateMarkerImage(moveMarker, segment1.Point.X, segment1.Point.Y);
                var segment2 = figure.Segments[1] as LineSegment;
                CreateMarkerImage(moveMarker, segment2.Point.X, segment2.Point.Y);
            }
        }


        private void InitialiseStretch(Line line)
        {
            ClearImages();
            editMode = EditMode.Stretch;
            var trendLine = line.Tag as Data.TrendLine;

            var startMarker = new Marker()
            {
                TrendLine = trendLine,
                Line = line,
                MarkerType = MarkerType.Start
            };
            CreateMarkerImage(startMarker, line.X1, line.Y1);

            var endMarker = new Marker()
            {
                TrendLine = trendLine,
                Line = line,
                MarkerType = MarkerType.End
            };
            CreateMarkerImage(endMarker, line.X2, line.Y2);
        }



        private void InitialiseStretch(Path path)
        {
            ClearImages();
            editMode = EditMode.Stretch;
            var trendLine = path.Tag as Data.TrendLine;

            var pathGeometry = path.Data as PathGeometry;
            var figure = pathGeometry.Figures[0];
            Point point1, point2;

            if (trendLine.TrendKind == Data.TrendKind.Quadratic)
            {
                var segment = figure.Segments[0] as QuadraticBezierSegment;
                point1 = segment.Point1;
                point2 = segment.Point2;
            }

            if (trendLine.TrendKind == Data.TrendKind.Measure)
            {
                var segment1 = figure.Segments[0] as LineSegment;
                point1 = segment1.Point;
                var segment2 = figure.Segments[1] as LineSegment;
                point2 = segment1.Point;
            }

            var startMarker = new Marker()
            {
                TrendLine = trendLine,
                Path = path,
                MarkerType = MarkerType.Start
            };
            CreateMarkerImage(startMarker, figure.StartPoint.X, figure.StartPoint.Y);

            var midMarker = new Marker()
            {
                TrendLine = trendLine,
                Path = path,
                MarkerType = MarkerType.Mid
            };
            CreateMarkerImage(midMarker, point1.X, point1.Y);

            var endMarker = new Marker()
            {
                TrendLine = trendLine,
                Path = path,
                MarkerType = MarkerType.End
            };
            CreateMarkerImage(endMarker, point2.X, point2.Y);
        }

        private void Stretch_Click(object sender, RoutedEventArgs e)
        {
            editMarker = null;


            if( sender is MenuItem menuItem)
            {
                var uiElement = ((ContextMenu)menuItem.Parent).PlacementTarget;

                if (uiElement is Line line)
                {
                    InitialiseStretch(line);
                }

                if (uiElement is Path path)
                {
                    InitialiseStretch(path);
                }

                editMode = EditMode.Stretch;
            }
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            editMarker = null;
            editMode = EditMode.None;

            if (sender is MenuItem menuItem)
            {
                var uiElement = ((ContextMenu)menuItem.Parent).PlacementTarget;


                if (uiElement is Line line)
                {
                    InitialiseMove(line);
                    editMode = EditMode.MoveLine;
                }

                if (uiElement is Path path)
                {
                    InitialiseMove(path);
                    editMode = EditMode.MoveLine;
                }

                if (uiElement is TextBox textBox)
                {
                    InitialiseMove(textBox);
                    editMode = EditMode.MoveLine;
                }
            }
        }



        private void CreateMarkerImage(Marker marker, double x, double y)
        {
            var original = marker.MarkerType == MarkerType.Move ? moveImage : selectionImage;
            TransformedBitmap bitmap = new TransformedBitmap();
            bitmap.BeginInit();
            bitmap.Source = original;
            bitmap.Transform = new ScaleTransform(10f/original.Width, 10f/original.Height);
            bitmap.EndInit();

            Image image = new Image() {
                Height = 15,
                Width = 15,
                Source = original,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                // Margin = new Thickness(0, 0, 0, 0),
                Stretch = Stretch.Fill,
                Tag = marker
            };

            image.MouseDown += Marker_MouseDown;

            // Create the shape and position
            canvas.Children.Add(image);
            Canvas.SetLeft(image, x-7.5);
            Canvas.SetTop(image, y-7.5);

            marker.Image = image;
        }


        private void Marker_MouseMove(object sender, MouseEventArgs e)
        {
            if (editMode == EditMode.None)
                return;

            if (sender is Image image)
            {

            }
        }

        private void Marker_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if( sender is Image image )
            {
                editMarker = image.Tag as Marker;
            }
        }

        override public void UpdateLayout()
        {
            editMode = EditMode.None;
            pointCount = 0;

            // Remove all images
            base.UpdateLayout();  
        }

        protected override void DrawDividendBackgrounds()
        {
            // Draw dividend backgrounds.  We want everything else to draw over the top of these
            // that's why they're first
            Pen pen = new Pen(dividendBrush, candleWidth * 2);

            for (int bar = FirstBar; bar <= LastBar; bar++)
            {
                DateTime barDate = traderBook.bars.Date[bar];
                foreach (Data.Dividend dividend in traderBook.Dividends)
                {
                    DateTime dividendDate = dividend.ExDividend;
                    if (barDate.Date == dividend.ExDividend.Date || (bar == LastBar && barDate.Date >= dividend.ExDividend.Date && barDate.AddDays(7).Date <= dividend.ExDividend.Date))
                    {
                        double x = BarToX(bar);
                        context.DrawLine(pen, new Point(x, 0), new Point(x, (float)canvas.ActualHeight - bottomBarHeight));
                    }
                }
            }
        }

        override protected void DrawTrendLines()
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
                    case Data.TrendKind.Measure:
                        DrawMeasureRule(line);
                        break;
                    default:
                        break;
                }
            }
        }

        protected void DrawScriptAnnotation(Data.Annotation annotation)
        {
            var bar = DateTimeToBar(annotation.Date);
            DrawScriptAnnotation(annotation, bar);
        }

        protected void DrawScriptAnnotation(Data.Annotation annotation, int bar)
        {
            var textBox = new TextBox();
            textBox.Text = annotation.Text.Trim();
            textBox.Background = Brushes.Transparent;

            textBox.FontFamily = systemFontFamily;

#pragma warning disable CS0618 // Type or member is obsolete

            var formattedText = new FormattedText(annotation.Text,
                System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                12,
                Brushes.Black);
#pragma warning restore CS0618 // Type or member is obsolete

            textBox.HorizontalAlignment = HorizontalAlignment.Center;
            textBox.VerticalAlignment = VerticalAlignment.Center;
            textBox.Tag = annotation;
            textBox.Width = formattedText.Width + 8;
            textBox.Height = formattedText.Height * 1.2;
            textBox.IsReadOnly = false;
            textBox.Focusable = true;
            textBox.ContextMenu = textMenu;
            textBox.AcceptsReturn = false;
            textBox.KeyDown += TextBox_KeyDown;
            textBox.KeyUp += TextBox_KeyUp;
            canvas.Children.Add(textBox);

            Canvas.SetLeft(textBox, BarToX(bar) - textBox.Width / 2f + 1);
            Canvas.SetTop(textBox, ValueToY(traderBook.TraderScript.PricePane, annotation.Price) - textBox.Height / 2 - 9);
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            var annotation = textBox.Tag as Data.Annotation;
            var bar = DateTimeToBar(annotation.Date);

            if (e.Key != Key.Return)
            {
                var formattedText = new FormattedText(textBox.Text,
                    System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                    12,
                    Brushes.Black);

                textBox.Width = formattedText.Width + 8;
                textBox.Height = formattedText.Height * 1.2;

                Canvas.SetLeft(textBox, BarToX(bar) - textBox.Width / 2f + 1);
                Canvas.SetTop(textBox, ValueToY(traderBook.TraderScript.PricePane, annotation.Price) - textBox.Height / 2 - 9);
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            var annotation = textBox.Tag as Data.Annotation;
            var bar = DateTimeToBar(annotation.Date);

            if (e.Key == Key.Return)
            {
                annotation.Text = textBox.Text.Trim();
                annotation.Save();
                canvas.Children.Remove(textBox);
                DrawScriptAnnotation(annotation);
            }
        }


        override protected void DrawScriptAnnotations()
        {
            // base.DrawScriptAnnotations();

            for (int bar = FirstBar; bar <= LastBar; bar++)
            {
                DateTime barDate = traderBook.TraderScript.bars.Date[bar];
                foreach (Data.Annotation annotation in traderBook.Annotations)
                {
                    DateTime annotationDate = annotation.Date;
                    if (barDate.Date == annotation.Date)
                    {
                        DrawScriptAnnotation(annotation, bar);                 
                    }
                }
            }
        }

        double DateTimeToX(DateTime date)
        {
            var bar = DateTimeToBar(date);
            return BarToX(bar);
        }

        int DateTimeToBar(DateTime needle)
        {
            int lastBar = traderBook.TraderScript.bars.Count-1;
            var date = traderBook.TraderScript.bars.Date;

            for (int i = lastBar; i >= 0; i--)
            {
                if (needle == date[i])
                {
                    return i;
                }
            }

            return -1;
        }

        override protected void DrawLinearTrendLine(Data.TrendLine trendLine)
        {
            bool foundStart = false;
            int barsCount = traderBook.TraderScript.bars.Count; // used for a bit of speed
            var date = traderBook.TraderScript.bars.Date; // used for a bit of speed
            var pricePane = traderBook.TraderScript.PricePane;

            int startBar = DateTimeToBar(trendLine.StartDate.Date);
            if (startBar == -1)
                return;

            int endBar = DateTimeToBar(trendLine.EndDate.Date); ;
            if (endBar == -1)
                return;

            if (startBar == endBar)
                return;

            // Find the slope and intercept. This needs to be done first, so we get the line right.
            double slope = (trendLine.EndPrice - trendLine.StartPrice) / (endBar - startBar);
            double intercept = trendLine.StartPrice - slope * startBar;

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
            double startPrice = startBar * slope + intercept;
            double endPrice = endBar * slope + intercept;

            var line = new Line();
            line.Stroke = blackBrush;
            line.StrokeThickness = lineStrokeThickness;
            line.X1 = BarToX(startBar);
            line.Y1 = ValueToY(pricePane, startPrice);
            line.X2 = BarToX(endBar);
            line.Y2 = ValueToY(pricePane, endPrice);
            line.Tag = trendLine;
            line.ContextMenu = lineMenu;

            canvas.Children.Add(line);
        }

        override protected void DrawQuadraticTrendLine(Data.TrendLine trendLine)
        {

            int startBar = 0, midBar = 0, endBar = 0;

            for (int i = 0; i < traderBook.bars.Count; i++)
            {
                DateTime date = traderBook.bars.Date[i];
                if (trendLine.StartDate.Date == date.Date)
                    startBar = i;
                if (trendLine.MidDate.Date == date.Date)
                    midBar = i;
                if (trendLine.MidDate.Date == date.Date)
                    midBar = i;
                if (trendLine.EndDate.Date == date.Date)
                    endBar = i;
            }

            var startPt = new Point(BarToX(startBar), ValueToY(traderBook.TraderScript.PricePane, trendLine.StartPrice));
            var midPt = new Point(BarToX(midBar), ValueToY(traderBook.TraderScript.PricePane, trendLine.MidPrice));
            var endPt = new Point(BarToX(endBar), ValueToY(traderBook.TraderScript.PricePane, trendLine.EndPrice));

            var path = new System.Windows.Shapes.Path() { Stroke = Brushes.Black, StrokeThickness = lineStrokeThickness };
            path.Tag = trendLine;
            var pathGeometry = new PathGeometry();
            path.Data = pathGeometry;
            var segment = new QuadraticBezierSegment(midPt, endPt, true);
            var segmentCollection = new PathSegmentCollection() { segment };
            var pathFigure = new PathFigure(startPt, segmentCollection, false);

            pathGeometry.Figures = new PathFigureCollection() { pathFigure };

            path.ContextMenu = lineMenu;
            canvas.Children.Add(path);
        }

        protected void DrawMeasureRule(Data.TrendLine trendLine)
        {

            int startBar = 0, midBar = 0, endBar = 0;

            for (int i = 0; i < traderBook.bars.Count; i++)
            {
                DateTime date = traderBook.bars.Date[i];
                if (trendLine.StartDate.Date == date.Date)
                    startBar = i;
                if (trendLine.MidDate.Date == date.Date)
                    midBar = i;
                if (trendLine.MidDate.Date == date.Date)
                    midBar = i;
                if (trendLine.EndDate.Date == date.Date)
                    endBar = i;
            }

            var startPt = new Point(BarToX(startBar), ValueToY(traderBook.TraderScript.PricePane, trendLine.StartPrice));
            var midPt = new Point(BarToX(midBar), ValueToY(traderBook.TraderScript.PricePane, trendLine.MidPrice));
            var endPt = new Point(BarToX(endBar), ValueToY(traderBook.TraderScript.PricePane, trendLine.EndPrice));

            var path = new System.Windows.Shapes.Path() { Stroke = Brushes.Blue, StrokeThickness = lineStrokeThickness };
            path.Tag = trendLine;
            var pathGeometry = new PathGeometry();
            path.Data = pathGeometry;
            var segment1 = new LineSegment(midPt, true);
            var segment2 = new LineSegment(endPt, true);
            var segmentCollection = new PathSegmentCollection() { segment1, segment2 };
            var pathFigure = new PathFigure(startPt, segmentCollection, false);

            pathGeometry.Figures = new PathFigureCollection() { pathFigure };

            path.ContextMenu = measureMenu;
            canvas.Children.Add(path);
        }


    }
}
