using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GlueNet.Vision.PTOT.WaferInspection.ImageSender.WpfApp
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ImageSenderViewModel ImageSenderViewModel { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public MainWindow()
        {
            InitializeComponent();

            ImageSenderViewModel = new ImageSenderViewModel();
        }


        private void Clear_OnClick(object sender, RoutedEventArgs e)
        {
            ImageSenderViewModel.ImageDownloader.Clear();
        }

        private void StartMonitor_OnClick(object sender, RoutedEventArgs e)
        {
            ImageSenderViewModel.ImageDownloader.StartMonitor();
        }

        private void StopMonitor_OnClick(object sender, RoutedEventArgs e)
        {
            ImageSenderViewModel.ImageDownloader.StopMonitor();
        }

        private void SetColumnCount_OnClick(object sender, RoutedEventArgs e)
        {
            ImageSenderViewModel.ImageDownloader.SetColumnNumber(ImageSenderViewModel.ColumnNumber);
        }
    }
}
