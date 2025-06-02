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
        private string myCurrentArchivePath;

        private string mySharedFolder = AppSettingsMgt.AppSettings.SharedFolder;

        private string myArchiveFolder = AppSettingsMgt.AppSettings.ArchiveFolder;

        private ManualResetEvent myManualResetEvent = new ManualResetEvent(false);

        //public TcpImageServer TcpImageServer { get; set; }

        public AiDetector AiDetector { get; set; }
        public ObservableCollection<DyeResult> DyeResultList { get; set; }
        public ObservableCollection<string> ImageFiles { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageReceiverViewModel()
        {
            //TcpImageServer = new TcpImageServer();

            var projectPath = @"A:\TestModel\Test_20250527.vfmodel";

            AiDetector = new AiDetector(projectPath);

            ImageFiles = new ObservableCollection<string>();

            DyeResultList = new ObservableCollection<DyeResult>();

            Task.Run(async() =>
            {
                await AiDetector.Initialize();

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

                    Task.Delay(10).Wait();
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

        public void CreateArchiveFolder()
        {
            myCurrentArchivePath = $"{myArchiveFolder}\\{DateTime.Now:yyyyMMdd_HHmmss}";

            Directory.CreateDirectory(myCurrentArchivePath);
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
                        var dyeResult = await AiDetector.Run(file);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DyeResultList.Add(dyeResult);
                        });

                        var index = dyeResult.Name.Split('.').FirstOrDefault();

                        var fileName = $"{index}_{dyeResult.Section}_{dyeResult.Column}_{dyeResult.Row}_{dyeResult.OKNG}.bmp";

                        var fullPath = Path.Combine(myCurrentArchivePath, fileName);

                        await Task.Run(() =>
                        {
                            File.Copy(file, fullPath, true);

                            CsvManager.AppendLog(dyeResult);
                        });
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

        public void ClearSharedFolder()
        {
            if (Directory.Exists(mySharedFolder))
            {
                var files = Directory.GetFiles(mySharedFolder, "*.bmp");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }

            ImageFiles.Clear();
            DyeResultList.Clear();
        }
    }
}
