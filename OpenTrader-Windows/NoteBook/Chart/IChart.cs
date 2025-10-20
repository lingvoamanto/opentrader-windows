using System;
namespace OpenTrader
{
    public interface IChart
    {
        // public void PlotPositions();
        // public void DrawAnnotationShapes();
        // public void DrawScriptAnnotations();
        // public void DrawLinearTrendLine(Data.TrendLine line);
        // public void DrawQuadraticTrendLine(Data.TrendLine line);
        // public void DrawTrendLines();
        // public void DrawHandAnnotations();
        // public void DrawDividendBackgrounds();
        // public void DrawMonthLines();
        public void executeDrawHorzLine(Pane pane, double width, double height, object[] parameters);
        public void executeDrawLine(Pane pane, double width, double height, object[] parameters);
        public void executeDrawPolygon(Pane pane, double width, double height, object[] parameters);
    }
}
