using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GlueNet.Vision.PTOT.WaferInspection;

namespace GlueNet.Vision.PTOT.WaferInspection.ImageSender.WpfApp
{
    public class ImageSenderViewModel : INotifyPropertyChanged
    {
        private string mySourceFolder = AppSettingsMgt.AppSettings.TcpConnectionSetting.SourceFolder;

        private ManualResetEvent myManualResetEvent = new ManualResetEvent(true);
        public int RowNumber { get; set; } = 17;
        public ImageDownloader ImageDownloader { get; set; }
        public TcpImageClient TcpImageClient { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSenderViewModel()
        {
            ImageDownloader = new ImageDownloader();

            ImageDownloader.SetRowNumber(RowNumber);

            ImageDownloader.ImageFiles = new ObservableCollection<string>();

            ImageDownloader.ImageFiles.CollectionChanged += ImageFiles_CollectionChanged;

            Task.Run(() =>
            {
                while (true)
                {
                    myManualResetEvent.WaitOne();

                    try
                    {
                        ScanFolder();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    Task.Delay(100).Wait();
                }
            });
        }

        private void ImageFiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(mySourceFolder))
            {
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var newImageFiles = e.NewItems.Cast<string>().ToList();

                newImageFiles.ForEach(file =>
                {
                    var isFileAvailable = WaitUntilFileIsReady(file, 1000);
                    if (File.Exists(file) && isFileAvailable)
                    {
                        ImageDownloader.Download(file);
                    }
                });
            }
        }

        private bool WaitUntilFileIsReady(string filePath, int timeoutMs = 1000)
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                    Task.Delay(10).Wait();
                }
            }

            MessageBox.Show("File is not ready: " + filePath);

            return false;
        }

        public void ScanFolder()
        {
            if (!string.IsNullOrEmpty(mySourceFolder))
            {
                var allFiles = Directory.GetFiles(mySourceFolder, "*.bmp");

                var excepts = allFiles.Except(ImageDownloader.ImageFiles).ToList();

                foreach (string except in excepts)
                {
                    ImageDownloader.ImageFiles.Add(except);
                }

                if (allFiles.Count() == 0)
                {
                    ImageDownloader.ImageFiles.Clear();
                }
            }
        }

        public void StartMonitor()
        {
            myManualResetEvent.Set();
        }

        public void StopMonitor()
        {
            myManualResetEvent.Reset();
        }
    }
}
