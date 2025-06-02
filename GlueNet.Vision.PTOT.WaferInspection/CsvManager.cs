using CsvHelper.Configuration;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueNet.Vision.PTOT.WaferInspection
{
    public static class CsvManager
    {
        private static string myFilePath { get; set; }
        public static void CreateNewFile(string folderPath, string filePath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            myFilePath = filePath;

            using (var stream = new FileStream(filePath, FileMode.CreateNew))
            {
                using (var writter = new StreamWriter(stream, new UTF8Encoding(true)))
                {
                    var header = "FileName,Section,Column,Row,OKNG,AiDetectResult";
                    writter.WriteLine(header);
                    writter.Flush();
                }
            }
        }

        public static void AppendLog(DyeResult dyeResult)
        {
            if (File.Exists(myFilePath))
            {
                try
                {
                    using (var writter = new StreamWriter(myFilePath, true, new UTF8Encoding(true)))
                    {
                        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            HasHeaderRecord = false,
                            ShouldQuote = args => true
                        };

                        using (var csv = new CsvWriter(writter, config))
                        {
                            //var content = $"{dyeResult.Name},{dyeResult.Section},{dyeResult.Column},{dyeResult.Row},{dyeResult.OKNG},{dyeResult.AiDetectResult}";
                            //csv.WriteField(content);

                            csv.WriteRecord(dyeResult);
                            csv.NextRecord();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    //LogMgt.Logger.Info(ex.Message);
                }
            }
        }
    }
}
