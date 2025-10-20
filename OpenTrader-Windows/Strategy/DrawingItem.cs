using System;
namespace OpenTrader
{
    public class DrawingItem
    {
        public DrawingMethod drawingmethod;
        public Pane pane;
        public object[] parameters;
        public bool behind = false;
    }
}
