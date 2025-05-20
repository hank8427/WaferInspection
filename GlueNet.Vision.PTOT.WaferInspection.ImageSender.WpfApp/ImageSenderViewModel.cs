using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlueNet.Vision.PTOT.WaferInspection;

namespace GlueNet.Vision.PTOT.WaferInspection.ImageSender.WpfApp
{
    public class ImageSenderViewModel : INotifyPropertyChanged
    {
        public int RowNumber { get; set; } = 17;
        public ImageDownloader ImageDownloader { get; set; }
        public TcpImageClient TcpImageClient { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSenderViewModel()
        {
            ImageDownloader = new ImageDownloader();

            ImageDownloader.SetRowNumber(RowNumber);

            //var folderToMonitor = AppSettingsMgt.AppSettings.TcpConnectionSetting.SenderFolder;

            //TcpImageClient = new TcpImageClient(folderToMonitor);
        }
    }
}
