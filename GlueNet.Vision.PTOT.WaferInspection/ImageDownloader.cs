using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GlueNet.Vision.PTOT.WaferInspection
{
    public class ImageDownloader
    {
        //private string SenderFolder = AppSettingsMgt.AppSettings.TcpConnectionSetting.SenderFolder;
        private ObservableCollection<string> myCopyOKImageFiles { get; set; } = new ObservableCollection<string>();

        private string mySharedFolder = AppSettingsMgt.AppSettings.SharedFolder;

        private int myTotalImageCount;

        private int myRowNumber;
        public ObservableCollection<string> ImageFiles { get; set; }

        public ImageDownloader()
        {

        }

        public void SetRowNumber(int rowNumber)
        {
            myRowNumber = rowNumber;
        }

        public void Download(string file)
        {   
            var fileName = Path.GetFileNameWithoutExtension(file);

            //var parseInt = int.TryParse(fileName, out int fileNumber);

            var parseInt = GetNumberFromFileName(fileName, out int fileNumber);

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

            var fullPath = Path.Combine(mySharedFolder, fileName);

            File.Copy(file, fullPath, true);
        }

        public async void DownloadAsync(string file)
        {
            try
            {
                if (!IsFileAccessible(file))
                {
                    return;
                }

                var fileName = Path.GetFileNameWithoutExtension(file);

                var parseInt = GetNumberFromFileName(fileName, out int fileNumber);

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
                    throw new Exception("File name parse error");
                }

                ImageFiles.Add(file);

                var fullPath = Path.Combine(mySharedFolder, fileName);

                var targetDirectory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }


                // 非同步複製檔案
                using (var sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var targetStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await sourceStream.CopyToAsync(targetStream);
                }

                myCopyOKImageFiles.Add(file);
                
            }
            catch (Exception ex)
            {
                ImageFiles.Remove(file);

                myTotalImageCount -= 1;

                Console.WriteLine($"Error downloading file {file}: {ex.Message}");
            }
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


        public void Clear()
        {
            myTotalImageCount = 0;

            myCopyOKImageFiles.Clear();
            ImageFiles.Clear();
        }

        public void Reset()
        {
            myCopyOKImageFiles.Clear();
            ImageFiles.Clear();
        }

        private bool GetNumberFromFileName(string fileName, out int fileNumber)
        {
            var match = Regex.Match(fileName, @"\d+");
            if (match.Success)
            {
                fileNumber = int.Parse(match.Value);
                return true;
            }

            fileNumber = -1;
            return false;
        }
    }
}
