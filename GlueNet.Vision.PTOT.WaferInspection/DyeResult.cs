using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueNet.Vision.PTOT.WaferInspection
{
    public class DyeResult
    {
        public string Name { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public int Section { get; set; }

        public string AiDetectResult { get; set; }
    }
}
