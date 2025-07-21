using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GlueNet.Vision.PTOT.Inspection
{
    public class TcpImageClient
    {
        private string mySenderFolder = AppSettingsMgt.AppSettings.TcpConnectionSetting.SenderFolder;

        private string myServerIp = AppSettingsMgt.AppSettings.TcpConnectionSetting.ServerIp;

        private int myServerPort = AppSettingsMgt.AppSettings.TcpConnectionSetting.ServerPort;

        private FileSystemWatcher myFileWatcher;
        public TcpImageClient(string folderToWatch)
        {
            mySenderFolder = folderToWatch;

            myFileWatcher = new FileSystemWatcher(mySenderFolder, "*.bmp");
            myFileWatcher.Created += OnImageCreated;
            myFileWatcher.EnableRaisingEvents = true;
        }

        private void OnImageCreated(object sender, FileSystemEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    WaitUntilFileIsReady(e.FullPath);

                    SendFile(e.FullPath);

                    Console.WriteLine($"Sent: {e.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending {e.Name}: {ex.Message}");
                }
            });
        }

        private void WaitUntilFileIsReady(string filePath)
        {
            while (true)
            {
                try
                {
                    using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        if (stream.Length > 0)
                            break;
                    }
                }
                catch (IOException)
                {
                    Task.Delay(50).Wait();
                }
            }
        }

        private void SendFile(string filePath)
        {
            try
            {
                using (var client = new TcpClient(myServerIp, myServerPort))
                {
                    using (var stream = client.GetStream())
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            string fileName = Path.GetFileName(filePath);
                            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                            byte[] fileData = File.ReadAllBytes(filePath);

                            // 傳送檔名長度、檔名、檔案長度、檔案內容
                            writer.Write(fileNameBytes.Length);
                            writer.Write(fileNameBytes);
                            writer.Write(fileData.Length);
                            writer.Write(fileData);

                            Console.WriteLine($"Sent file: {fileName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send failed: {ex.Message}");
            }
        }
    }
}
