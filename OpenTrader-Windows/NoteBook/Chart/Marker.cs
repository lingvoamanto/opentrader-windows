using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace OpenTrader
{
    enum MarkerType { Move = 0, Start = 1, Mid = 2, End = 3 }
    class Marker
    {
        object data;
        public Data.TrendLine TrendLine { get=>data as Data.TrendLine; set=>data=value; }

        public Data.Annotation Annotation { get=>data as Data.Annotation; set=>data=value; }

        object element;
        public Line Line { get=>element as Line; set=>element=value; }
        public Path Path { get => element as Path; set => element = value; }
        public TextBox TextBox { get => element as TextBox; set => element = value; }

        public Image Image { get; set; }

        public MarkerType MarkerType { get; set;  }
    }
}
