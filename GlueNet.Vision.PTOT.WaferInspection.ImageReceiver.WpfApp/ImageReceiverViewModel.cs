using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueNet.Vision.PTOT.WaferInspection.ImageReceiver.WpfApp
{
    public class ImageReceiverViewModel : INotifyPropertyChanged
    {
        public TcpImageServer TcpImageServer { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageReceiverViewModel()
        {
            TcpImageServer = new TcpImageServer();
        }
    }
}
