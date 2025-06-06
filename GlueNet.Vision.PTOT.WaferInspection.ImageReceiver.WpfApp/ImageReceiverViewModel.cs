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
using OpenCvSharp;
using OpenCvSharp.Extensions;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace GlueNet.Vision.PTOT.WaferInspection.ImageReceiver.WpfApp
{
    public class ImageReceiverViewModel : INotifyPropertyChanged
    {
        private Stopwatch myStopwatch = new Stopwatch();

        private string myCurrentArchivePath;

        private string myCurrentLabelImagesPath;

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

            var projectPath = @"A:\TestModel\無網印Test.vfmodel";

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
                        //ScanFolder2();

                        ScanFolder();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
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
                var allFiles = Directory.GetFiles(mySharedFolder, "*.bmp")
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

                        var fullPath = Path.Combine(myCurrentArchivePath, fileName);

                        await Task.Run(() =>
                        {
                            File.Copy(file, fullPath, true);

                            CsvManager.AppendLog(dyeResult);


                            //using (var bitmap = new Bitmap(file))
                            //{
                            //    var dyeDefectList =
                            //        JsonConvert.DeserializeObject<List<DyeDefect>>(dyeResult.AiDetectResult);

                            //    var labelImage = DrawOnImage(dyeDefectList, bitmap);

                            //    var labelImagePath = Path.Combine(myCurrentLabelImagesPath, $"{index}_label.bmp");

                            //    if (!Directory.Exists(myCurrentLabelImagesPath))
                            //    {
                            //        Directory.CreateDirectory(myCurrentLabelImagesPath);
                            //    }

                            //    labelImage.Save(labelImagePath);
                            //}
                        });
                    }
                });

                if (allFiles.Count() == 0)
                {
                    ImageFiles.Clear();
                }
            }
        }

        public void ScanFolder2()
        {
            Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(mySharedFolder))
                {
                    var allFiles = Directory.GetFiles(mySharedFolder, "*.bmp")
                        .OrderBy(f =>
                        {
                            string fileName = Path.GetFileNameWithoutExtension(f);
                            return int.TryParse(fileName, out int num) ? num : int.MaxValue;
                        }).ToList();

                    allFiles.ForEach(async file =>
                    {
                        if (!IsFileAccessible(file))
                        {
                            return;
                        }

                        if (File.Exists(file))
                        {
                            var fileName = Path.GetFileName(file);

                            var fullPath = Path.Combine("D:\\TestShare", fileName);

                            myStopwatch.Restart();

                            File.Move(file, fullPath);

                            myStopwatch.Stop();
                            Console.WriteLine($@"Move File Time: {myStopwatch.Elapsed.TotalMilliseconds} milliseconds");
                        }
                    });
                }
            });
        }

        public void CreateArchiveFolder()
        {
            var dateTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            myCurrentArchivePath = $"{myArchiveFolder}\\{dateTime}";

            myCurrentLabelImagesPath = $"D:\\LabelImages\\{dateTime}";

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

        private bool WaitUntilFileIsReady(string filePath, int timeoutMs = 2000)
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
