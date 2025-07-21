using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace GlueNet.Vision.PTOT.Inspection
{
    public class DyeDefect
    {
        public string ClassName { get; set; }
        public Rectangle Rectangle { get; set; }
        public double Confidence { get; set; }
        public Point2f[] Points { get; set;}

        public DyeDefect(string className, Rectangle rectangle, double confidence, Point2f[] points)
        {
            ClassName = className;
            Rectangle = rectangle;
            Confidence = confidence;
            Points = points;
        }
    }

    public class Rectangle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public Rectangle(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
