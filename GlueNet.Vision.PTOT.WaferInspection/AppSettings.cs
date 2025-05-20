using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace GlueNet.Vision.PTOT.WaferInspection
{
    public class AppSettings
    {
        public TcpConnectionSetting TcpConnectionSetting { get; set; }
    }

    public class TcpConnectionSetting
    {
        public string ServerIp { get; set; }
        public int ServerPort { get; set; }
        public string SourceFolder { get; set; }
        public string SenderFolder { get; set; }
        public string ReceiverFolder { get; set; }
        public string SharedFolder { get; set; }
    }
}
