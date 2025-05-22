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
        //private string SenderFolder = AppSettingsMgt.AppSettings.TcpConnectionSetting.SenderFolder;

        private string mySharedFolder = AppSettingsMgt.AppSettings.SharedFolder;

        private int myTotalImageCount;

        private int myRowNumber;
        public ObservableCollection<string> ImageFiles { get; set; }

        public ImageDownloader()
        {
            var test = Directory.Exists(mySharedFolder);
        }

        public void SetRowNumber(int rowNumber)
        {
            myRowNumber = rowNumber;
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

            var fullPath = Path.Combine(mySharedFolder, fileName);

            File.Copy(file, fullPath, true);
        }

        public void Clear()
        {
            myTotalImageCount = 0;
        }
    }
}
