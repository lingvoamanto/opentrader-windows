using System;
using System.Drawing;
using System.Collections.Generic;

namespace OpenTrader
{
    public class ColorString
    {
        public string Text;
        public System.Drawing.Color Color;
        public System.Drawing.Color Background;
        public Font Font;
        public bool Above;

        public ColorString()
        {
            Background = System.Drawing.Color.Transparent;
            Font = null;
            Above = true;
        }
    }


    public class ShapeDetail
    {
        public double Price;
        public System.Drawing.Color Color;
        public Shape Shape;

        public ShapeDetail(Shape shape, double price, System.Drawing.Color color)
        {
            Shape = shape;
            Price = price;
            Color = color;
        }
    }

    public enum Shape
    {
        Circle, Square, UpTriangle, DownTriangle
    }

    public class Annotation
    {
        public List<ColorString> colorstring;
        public List<Position> position;
        public System.Drawing.Color backgroundcolor;
        public List<ShapeDetail> shapes;

        public Annotation()
        {
            colorstring = new List<ColorString>();
            position = new List<Position>();
            backgroundcolor = System.Drawing.Color.Transparent;
            shapes = new List<ShapeDetail>();
        }

        public double bottom;  //used for calculating where the next annotation is to go
        public double top; // used for calculating where the next annotation is to go
    }

    public class Annotations
    {
        private Annotation[] mAnnotations;

        public Annotations(int size)
        {
            mAnnotations = new Annotation[size];
        }

        public Annotation this[int index]
        {
            get
            {
                int _index = Math.Min(index, mAnnotations.Length - 1);
                if (_index == -1)
                    return null;
                if (mAnnotations[_index] == null)
                    mAnnotations[_index] = new Annotation();
                return mAnnotations[_index];
            }
        }
    }
}

