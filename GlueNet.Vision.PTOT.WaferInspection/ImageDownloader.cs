using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GlueNet.Vision.PTOT.WaferInspection
{
    public class ImageDownloader
    {
        private string SourceFolder = AppSettingsMgt.AppSettings.TcpConnectionSetting.SourceFolder;

        private string SenderFolder = AppSettingsMgt.AppSettings.TcpConnectionSetting.SenderFolder;

        private int myTotalImageCount;

        private int myRowNumber;

        private ManualResetEvent myManualResetEvent = new ManualResetEvent(false);
        public ObservableCollection<string> ImageFiles { get; set; }

        public ImageDownloader()
        {
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

        private void ImageFiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(SourceFolder))
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
                        Download(file);
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

        public void SetRowNumber(int rowNumber)
        {
            myRowNumber = rowNumber;
        }

        public void ScanFolder()
        {
            if (!string.IsNullOrEmpty(SourceFolder))
            {
                var allFiles = Directory.GetFiles(SourceFolder, "*.bmp");

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

        public void Download(string file)
        {   
            var fileName = Path.GetFileNameWithoutExtension(file);

            var parseInt = int.TryParse(fileName, out int fileNumber);

            if (parseInt)
            {
                myTotalImageCount += 1;
                var row = myTotalImageCount / myRowNumber;

                if (myTotalImageCount % myRowNumber == 0)
                {
                    row -= 1;
                }

                fileName = (row * myRowNumber + fileNumber).ToString() + ".bmp";
            }
            else
            {
                // Log error
                MessageBox.Show("File name parse error");
            }

            var fullPath = Path.Combine(SenderFolder, fileName);

            File.Copy(file, fullPath, true);
        }

        public void Clear()
        {
            myTotalImageCount = 0;
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
