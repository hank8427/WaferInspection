using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace GlueNet.Vision.PTOT.WaferInspection
{
    public class DyeDefect
    {
        public string ClassName { get; set; }
        public Rectangle Rectangle { get; set; }
        public double Confidence { get; set; }

        public DyeDefect(string className, Rectangle rectangle, double confidence)
        {
            ClassName = className;
            Rectangle = rectangle;
            Confidence = confidence;
        }
    }

    public class Rectangle
    {
        private float myPixelSize = 1.4f;
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public Rectangle(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width * myPixelSize;
            Height = height * myPixelSize;
        }
    }
}
