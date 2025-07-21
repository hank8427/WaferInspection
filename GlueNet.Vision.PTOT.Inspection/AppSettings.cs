using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace GlueNet.Vision.PTOT.Inspection
{
    public class AppSettings
    {
        public int SectionNumber { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string SharedFolder { get; set; }
        public string ArchiveFolder { get; set; }
        public string CsvOutputFolder { get; set; }
        public TcpConnectionSetting TcpConnectionSetting { get; set; }
    }

    public class TcpConnectionSetting
    {
        public string ServerIp { get; set; }
        public int ServerPort { get; set; }
        public string SourceFolder { get; set; }
        public string SenderFolder { get; set; }
        public string ReceiverFolder { get; set; }

    }
}
