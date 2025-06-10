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
        private bool myIsFirstBatch = true;

        private int myCurrentColumn = 0;

        private string mySourceFolder = AppSettingsMgt.AppSettings.TcpConnectionSetting.SourceFolder;

        private ManualResetEvent myManualResetEvent = new ManualResetEvent(false);
        public int SectionNumber { get; set; } = AppSettingsMgt.AppSettings.SectionNumber;
        public int RowNumber { get; set; } = AppSettingsMgt.AppSettings.RowNumber;
        public int ColumnNumber { get; set; } = AppSettingsMgt.AppSettings.ColumnNumber;
        public ImageDownloader ImageDownloader { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSenderViewModel()
        {
            ImageDownloader = new ImageDownloader();

            ImageDownloader.SetSize(SectionNumber, ColumnNumber, RowNumber);

            ImageDownloader.ImageFiles = new ObservableCollection<string>();

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

                    Task.Delay(1).Wait();
                }
            });
        }

        public void ScanFolder()
        {
            if (!string.IsNullOrEmpty(mySourceFolder))
            {
                var allFiles = Directory.GetFiles(mySourceFolder, "*.bmp");

                var excludeFiles = allFiles.Where(x => x.Contains("-")).ToList();

                var restFiles = allFiles.Except(excludeFiles).ToList();

                if (restFiles.Count == 0)
                {
                    ImageDownloader.Reset();
                    myIsFirstBatch = false;
                    myCurrentColumn += 1;
                }
                else
                {
                    if (!myIsFirstBatch)
                    {
                        var excepts = restFiles.Except(ImageDownloader.ImageFiles).ToList();

                        foreach (string except in excepts)
                        {
                            ImageDownloader.DownloadAsync(except, myCurrentColumn);
                        }
                    }
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

        public void Clear()
        {
            ImageDownloader.Clear();
            myIsFirstBatch = true;
        }

        public void SetSize()
        {
            ImageDownloader.SetSize(SectionNumber, ColumnNumber, RowNumber);

            AppSettingsMgt.AppSettings.SectionNumber = SectionNumber;
            AppSettingsMgt.AppSettings.RowNumber = RowNumber;
            AppSettingsMgt.AppSettings.ColumnNumber = ColumnNumber;
            AppSettingsMgt.Save();
        }
    }
}
