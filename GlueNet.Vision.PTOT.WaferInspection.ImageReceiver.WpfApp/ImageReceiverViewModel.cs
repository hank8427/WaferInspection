using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using GlueNet.VisionAI.Core.Models;
using GlueNet.VisionAI.Core.Operations;
using Newtonsoft.Json;
using NLog;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace GlueNet.Vision.PTOT.WaferInspection.ImageReceiver.WpfApp
{
    public class ImageReceiverViewModel : INotifyPropertyChanged
    {
        private ILogger myLogger = LogManager.GetCurrentClassLogger();

        private Stopwatch myStopwatch = new Stopwatch();

        private string myCurrentArchivePath;

        private string myCurrentLabelImagesPath;

        private string mySharedFolder = AppSettingsMgt.AppSettings.SharedFolder;

        private string myArchiveFolder = AppSettingsMgt.AppSettings.ArchiveFolder;

        private ManualResetEvent myManualResetEvent = new ManualResetEvent(false);

        //public TcpImageServer TcpImageServer { get; set; }
        public int SectionNumber { get; set; } = AppSettingsMgt.AppSettings.SectionNumber;
        public int RowNumber { get; set; } = AppSettingsMgt.AppSettings.RowNumber;
        public int ColumnNumber { get; set; } = AppSettingsMgt.AppSettings.ColumnNumber;
        public AiDetector AiDetector { get; set; }
        public string CurrentSourceFolder { get; set; }
        public ObservableCollection<DyeResult> DyeResultList { get; set; }
        public ObservableCollection<string> ImageFiles { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageReceiverViewModel()
        {
            //TcpImageServer = new TcpImageServer();

            var projectPath = @"A:\TestModel\無網印Test.vfmodel";

            AiDetector = new AiDetector(projectPath);

            AiDetector.SetSize(SectionNumber, ColumnNumber, RowNumber);

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
                        //ScanFolder2();

                        ScanFolder();
                    }
                    catch (Exception ex)
                    {

                        myLogger.Info(ex.ToString());
                    }

                    Task.Delay(10).Wait();
                }
            });

            //ImageFiles.CollectionChanged += ImageFiles_CollectionChanged;
        }

        public void ScanFolder()
        {
            if (!string.IsNullOrEmpty(mySharedFolder))
            {
                var currentPathInfo = Directory.GetDirectories(mySharedFolder)
                                    .Select(dir => new DirectoryInfo(dir))
                                    .OrderBy(dirInfo => dirInfo.CreationTime)
                                    .FirstOrDefault();


                if (currentPathInfo?.FullName == null)
                {
                    return;
                }

                CurrentSourceFolder = currentPathInfo.Name;

                var allFiles = Directory.GetFiles(currentPathInfo.FullName, "*.bmp")
                    .OrderBy(f =>
                    {
                        string fileName = Path.GetFileNameWithoutExtension(f);
                        return int.TryParse(fileName, out int num) ? num : int.MaxValue;
                    }).ToArray();

                var excepts = allFiles.Except(ImageFiles).ToList();

                excepts.ForEach(async file =>
                {
                    if (!IsFileAccessible(file))
                    {
                        return;
                    }

                    if (ImageFiles.Count == 0)
                    {
                        myStopwatch.Start();
                    }

                    if (File.Exists(file))
                    {
                        var dyeResult = await AiDetector.Run(file);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DyeResultList.Add(dyeResult);
                        });

                        ImageFiles.Add(file);

                        var index = dyeResult.Name.Split('.').FirstOrDefault();

                        var fileName = $"{index}_{dyeResult.Section}_{dyeResult.Column}_{dyeResult.Row}_{dyeResult.OKNG}.bmp";

                        var currentArchivePath = $"{myCurrentArchivePath}\\{currentPathInfo.Name}";

                        var fullPath = Path.Combine(currentArchivePath, fileName);

                        if (!Directory.Exists(currentArchivePath))
                        {
                            Directory.CreateDirectory(currentArchivePath);

                            var csvfolderPath = $"{myCurrentArchivePath}\\{currentPathInfo.Name}";
                            var csvfilePath = $"{csvfolderPath}\\{myCurrentArchivePath.Split('\\').LastOrDefault()}.csv";
                            CsvManager.CreateNewFile(csvfolderPath, csvfilePath);
                        }

                        await Task.Run(() =>
                        {
                            File.Copy(file, fullPath, true);

                            CsvManager.AppendLog(dyeResult);
                        });
                    }
                });

                if (ImageFiles.Count == SectionNumber*ColumnNumber*RowNumber)
                {
                    Directory.Delete(currentPathInfo.FullName, true);
                    ImageFiles.Clear();
                    DyeResultList.Clear();

                    myStopwatch.Stop();
                    myLogger.Info($"Time to complete detection of Frame {currentPathInfo.FullName} : {myStopwatch.Elapsed.TotalSeconds} seconds");
                }
            }
        }

        //public void ScanFolder2()
        //{
        //    if (!string.IsNullOrEmpty(mySharedFolder))
        //    {
        //        var allFiles = Directory.GetFiles(mySharedFolder, "*.bmp")
        //            .OrderBy(f =>
        //            {
        //                string fileName = Path.GetFileNameWithoutExtension(f);
        //                return int.TryParse(fileName, out int num) ? num : int.MaxValue;
        //            }).ToList();

        //        allFiles.ForEach(async file =>
        //        {
        //            if (!IsFileAccessible(file))
        //            {
        //                return;
        //            }

        //            if (File.Exists(file))
        //            {
        //                var fileName = Path.GetFileName(file);

        //                var fullPath = Path.Combine("D:\\TestShare", fileName);

        //                myStopwatch.Restart();

        //                File.Move(file, fullPath);

        //                myStopwatch.Stop();
        //                Console.WriteLine($@"Move File Time: {myStopwatch.Elapsed.TotalMilliseconds} milliseconds");
        //            }
        //        });
        //    }
        //}

        public void CreateArchiveFolder(string dateTime)
        {
            myCurrentArchivePath = $"{myArchiveFolder}\\{dateTime}";

            Directory.CreateDirectory(myCurrentArchivePath);
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

        public void SetSize()
        {
            AiDetector.SetSize(SectionNumber, ColumnNumber, RowNumber);

            AppSettingsMgt.AppSettings.SectionNumber = SectionNumber;
            AppSettingsMgt.AppSettings.RowNumber = RowNumber;
            AppSettingsMgt.AppSettings.ColumnNumber = ColumnNumber;
            AppSettingsMgt.Save();
        }

        private bool IsFileAccessible(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private Bitmap DrawOnImage(List<DyeDefect> dyeDefectList, Bitmap bitmap, bool selectItem = false)
        {
            Bitmap outputBitmap;
            bool needsConversion = bitmap.PixelFormat == PixelFormat.Format8bppIndexed;

            if (needsConversion)
            {
                outputBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(outputBitmap))
                {
                    g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                }
            }
            else
            {
                outputBitmap = bitmap;
            }

            //Bitmap outputBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(outputBitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                foreach (var dyeDefect in dyeDefectList)
                {
                    if (dyeDefect != null)
                    {
                        var bbox = dyeDefect.Rectangle;

                        using (Pen pen = new Pen(Color.Red, 2))
                        {
                            using (Brush brush = new SolidBrush(Color.Red))
                            {
                                Font font = new Font("Arial", 20);
                                g.DrawRectangle(pen, bbox.X, bbox.Y, bbox.Width, bbox.Height);
                                g.DrawString(dyeDefect.ClassName, font, brush, bbox.X - 20, bbox.Y - 50);
                            }
                        }
                    }
                }
            }
            return outputBitmap;
        }
    }
}
