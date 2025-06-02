using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueNet.Vision.PTOT.WaferInspection
{
    public class DyeDefect
    {
        public string ClassName { get; set; }
        public Rect2f Rectangle { get; set; }
        public double Confidence { get; set; }

        public DyeDefect(string className, Rect2f rectangle, double confidence)
        {
            ClassName = className;
            Rectangle = rectangle;
            Confidence = confidence;
        }
    }
}
