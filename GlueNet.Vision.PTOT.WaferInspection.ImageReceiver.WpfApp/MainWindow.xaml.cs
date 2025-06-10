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
using GlueNet.Vision.PTOT.WaferInspection.ImageSender.WpfApp;

namespace GlueNet.Vision.PTOT.WaferInspection.ImageReceiver.WpfApp
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ImageReceiverViewModel ImageReceiverViewModel { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public MainWindow()
        {
            InitializeComponent();

            ImageReceiverViewModel = new ImageReceiverViewModel();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            //await ImageReceiverViewModel.AiDetector.Initialize();
        }

        private void SetSize_OnClick(object sender, RoutedEventArgs e)
        {
            ImageReceiverViewModel.SetSize();
        }

        private void StartMonitor_OnClick(object sender, RoutedEventArgs e)
        {
            ImageReceiverViewModel.StartMonitor();
            
            var dateTime = DateTime.Now.ToString("yyyyMMddHHmmss");

            ImageReceiverViewModel.CreateArchiveFolder(dateTime);

            var csvfolderPath = AppSettingsMgt.AppSettings.ArchiveFolder;
            var csvfilePath = $"{csvfolderPath}\\{dateTime}\\{dateTime}.csv";
            CsvManager.CreateNewFile(csvfolderPath, csvfilePath);

        }

        private void StopMonitor_OnClick(object sender, RoutedEventArgs e)
        {
            ImageReceiverViewModel.StopMonitor();
        }

        private void ClearDyeResultList_OnClick(object sender, RoutedEventArgs e)
        {
            ImageReceiverViewModel.ClearSharedFolder();
        }
    }
}
