using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GlueNet.Vision.PTOT.WaferInspection
{
    public class TcpImageServer
    {
        private int myServerPort = AppSettingsMgt.AppSettings.TcpConnectionSetting.ServerPort;

        private string myReceiverFolder = AppSettingsMgt.AppSettings.TcpConnectionSetting.ReceiverFolder;

        public TcpImageServer()
        {
            Task.Run(() =>
            {
                TcpListener listener = new TcpListener(IPAddress.Any, myServerPort);
                listener.Start();
                Console.WriteLine($"Server listening on port {myServerPort}");

                while (true)
                {
                    var client = listener.AcceptTcpClient();
                    Task.Run(() => HandleClient(client));
                }
            });
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        int fileNameLength = reader.ReadInt32();
                        string fileName = Encoding.UTF8.GetString(reader.ReadBytes(fileNameLength));

                        int fileLength = reader.ReadInt32();
                        byte[] fileData = reader.ReadBytes(fileLength);

                        string filePath = Path.Combine(myReceiverFolder, fileName);
                        File.WriteAllBytes(filePath, fileData);

                        Console.WriteLine($"Received and saved: {fileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}
