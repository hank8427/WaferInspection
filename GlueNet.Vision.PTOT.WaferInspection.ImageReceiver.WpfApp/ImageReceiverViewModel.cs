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

namespace GlueNet.Vision.PTOT.WaferInspection.ImageReceiver.WpfApp
{
    public class ImageReceiverViewModel : INotifyPropertyChanged
    {
        private string mySharedFolder = AppSettingsMgt.AppSettings.SharedFolder;

        private ManualResetEvent myManualResetEvent = new ManualResetEvent(true);

        //public TcpImageServer TcpImageServer { get; set; }

        public AiDetector AiDetector { get; set; }
        public int RowNumber { get; set; } = AppSettingsMgt.AppSettings.RowNumber;
        public int ColumnNumber { get; set; } = AppSettingsMgt.AppSettings.ColumnNumber;

        public ObservableCollection<DyeResult> DyeResultList { get; set; }
        public ObservableCollection<string> ImageFiles { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageReceiverViewModel()
        {
            //TcpImageServer = new TcpImageServer();

            var projectPath = @"A:\TestModel\白金科技瑕疵檢測_20250519.vfmodel";

            AiDetector = new AiDetector(projectPath);


            ImageFiles = new ObservableCollection<string>();

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

            ImageFiles.CollectionChanged += ImageFiles_CollectionChanged;
        }

        public void ScanFolder()
        {
            if (!string.IsNullOrEmpty(mySharedFolder))
            {
                var allFiles = Directory.GetFiles(mySharedFolder, "*.bmp");

                var excepts = allFiles.Except(ImageFiles).ToList();

                foreach (string except in excepts)
                {
                    ImageFiles.Add(except);
                }

                if (allFiles.Count() == 0)
                {
                    ImageFiles.Clear();
                }
            }
        }

        private void ImageFiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(mySharedFolder))
            {
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var newImageFiles = e.NewItems.Cast<string>().ToList();

                newImageFiles.ForEach(async file =>
                {
                    var isFileAvailable = WaitUntilFileIsReady(file, 1000);
                    if (File.Exists(file) && isFileAvailable)
                    {
                        await AiDetector.Run(file);
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
