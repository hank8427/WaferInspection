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

        private Stopwatch myTotalStopwatch = new Stopwatch();

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
        public ObservableCollection<string> TempImageFiles { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageReceiverViewModel()
        {
            //TcpImageServer = new TcpImageServer();

            var projectPath = @"A:\TestModel\Test_20250619.vfmodel";

            AiDetector = new AiDetector(projectPath);

            AiDetector.SetSize(SectionNumber, ColumnNumber, RowNumber);

            ImageFiles = new ObservableCollection<string>();

            TempImageFiles = new ObservableCollection<string>();

            DyeResultList = new ObservableCollection<DyeResult>();

            Task.Run(async() =>
            {
                try
                {
                    await AiDetector.Initialize();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.ToString()}");
                }

                while (true)
                {
                    myManualResetEvent.WaitOne();

                    try
                    {
                        //await Task.Run(DownloadToTempFolder);

                        ScanFolder();
                    }
                    catch (Exception ex)
                    {

                        myLogger.Info(ex.ToString());
                    }

                    Task.Delay(10).Wait();
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    myManualResetEvent.WaitOne();

                    try
                    {
                        DownloadToTempFolder();
                    }
                    catch (Exception ex)
                    {

                        myLogger.Info(ex.ToString());
                    }

                    Task.Delay(5).Wait();
                }
            });

            //ImageFiles.CollectionChanged += ImageFiles_CollectionChanged;
        }

        public void ScanFolder()
        {
            if (!string.IsNullOrEmpty("D:\\TestShare\\"))
            //if (!string.IsNullOrEmpty(mySharedFolder))
            {
                var currentPathInfo = Directory.GetDirectories("D:\\TestShare\\")
                //var currentPathInfo = Directory.GetDirectories(mySharedFolder)
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

                    //if (ImageFiles.Count == 0)
                    //{
                    //    myTotalStopwatch.Start();
                    //}

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

                        Task.Run(() =>
                        {
                            if (File.Exists(file))
                            {
                                File.Copy(file, fullPath, true);
                            }
                            CsvManager.AppendLog(dyeResult);
                        });
                    }
                });

                if (ImageFiles.Count == SectionNumber*ColumnNumber*RowNumber)
                {
                    Directory.Delete(currentPathInfo.FullName, true);
                    ImageFiles.Clear();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DyeResultList.Clear();
                    });

                    myTotalStopwatch.Stop();

                    var message = $"Time to complete detection of {currentPathInfo.FullName.Split('\\').LastOrDefault()} : {myTotalStopwatch.Elapsed.TotalSeconds} seconds";

                    Console.WriteLine(message);

                    myLogger.Info(message);
                }
            }
        }

        public void ScanFolder_test()
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

                var dyeResultsBatch = new List<DyeResult>();
                var copyTasks = new List<Task>();

                var currentArchivePath = Path.Combine(myCurrentArchivePath, currentPathInfo.Name);
                if (!Directory.Exists(currentArchivePath))
                {
                    Directory.CreateDirectory(currentArchivePath);

                    var csvfilePath = Path.Combine(currentArchivePath, $"{myCurrentArchivePath.Split('\\').LastOrDefault()}.csv");
                    CsvManager.CreateNewFile(currentArchivePath, csvfilePath);
                }

                excepts.ForEach(async file =>
                {
                    if (!IsFileAccessible(file) || !File.Exists(file))
                        return;

                    if (ImageFiles.Count == 0)
                    {
                        myStopwatch.Start();
                    }

                    var newStopWatch = Stopwatch.StartNew();

                    byte[] bytes = File.ReadAllBytes(file);

                    var mat = Cv2.ImDecode(bytes, ImreadModes.Grayscale);

                    Task.Delay(1100).Wait();

                    newStopWatch.Stop();
                    Console.WriteLine($@"Create Mat Elapsed Time: {newStopWatch.Elapsed.TotalMilliseconds} milliseconds");

                    //var dyeResult = await AiDetector.Run(file);

                    ImageFiles.Add(file);

                    var testStopWatch = Stopwatch.StartNew();

                    await Task.Delay(700);

                    testStopWatch.Stop();
                    Console.WriteLine($@"AI Elapsed Time: {testStopWatch.Elapsed.TotalMilliseconds} milliseconds");

                    var dyeResult = new DyeResult()
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Section = 0,
                        Column = 0,
                        Row = 0,
                        OKNG = "OK",
                    };

                    dyeResultsBatch.Add(dyeResult);

                    var index = dyeResult.Name.Split('.').FirstOrDefault();

                    var fileName = $"{index}_{dyeResult.Section}_{dyeResult.Column}_{dyeResult.Row}_{dyeResult.OKNG}.bmp";
                    var fullPath = Path.Combine(currentArchivePath, fileName);

                    // 將複製與 Csv 紀錄放入 task 清單中，稍後批次執行
                    copyTasks.Add(Task.Run(() =>
                    {
                        if (File.Exists(file))
                        {
                            File.Copy(file, fullPath, true);
                        }
                        CsvManager.AppendLog(dyeResult);
                    }));
                });

                // 等待所有複製與寫檔任務完成
                Task.WaitAll(copyTasks.ToArray());

                // 一次更新 UI，避免多次 Dispatcher 呼叫
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var dr in dyeResultsBatch)
                    {
                        DyeResultList.Add(dr);
                    }
                });

                if (ImageFiles.Count == SectionNumber * ColumnNumber * RowNumber)
                {
                    Directory.Delete(currentPathInfo.FullName, true);
                    ImageFiles.Clear();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DyeResultList.Clear();
                    });

                    myStopwatch.Stop();
                    myLogger.Info($"Time to complete detection of Frame {currentPathInfo.FullName} : {myStopwatch.Elapsed.TotalSeconds} seconds");
                }
            }
        }

        public void DownloadToTempFolder()
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
                    }).ToList();

                //List<string> transFiles;

                //var rangeCount = 7;

                //if (allFiles.Count >= rangeCount)
                //{
                //    transFiles = allFiles.GetRange(0, rangeCount);
                //}
                //else
                //{
                //    transFiles = allFiles;
                //}

                allFiles.ForEach(async file =>
                {
                    if (!IsFileAccessible(file))
                    {
                        return;
                    }

                    if (TempImageFiles.Count == 0)
                    {
                        myTotalStopwatch.Start();
                    }

                    if (File.Exists(file))
                    {
                        TempImageFiles.Add(file);

                        var fileName = Path.GetFileName(file);

                        var newFolder = $"D:\\TestShare\\{currentPathInfo.Name}";

                        if (!Directory.Exists(newFolder))
                        {
                            Directory.CreateDirectory(newFolder);
                        }

                        var fullPath = Path.Combine(newFolder, fileName);

                        myStopwatch.Restart();

                        File.Move(file, fullPath);

                        myStopwatch.Stop();
                        Console.WriteLine($@"Move File Time: {myStopwatch.Elapsed.TotalMilliseconds} milliseconds");
                    }
                });

                if (TempImageFiles.Count == SectionNumber * ColumnNumber * RowNumber)
                {
                    Directory.Delete(currentPathInfo.FullName, true);
                    TempImageFiles.Clear();
                }
            }
        }

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
