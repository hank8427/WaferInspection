using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GlueNet.Vision.PTOT.WaferInspection
{
    public static class AppSettingsMgt
    {
        public const string SettingFile = "AppData\\AppSettings.Json";

        public static AppSettings AppSettings { get; set; }

        static AppSettingsMgt()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                var setting = new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                AppSettings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(SettingFile), setting);
            }
            catch (Exception ex)
            {
                var s = new StackTrace(ex);
                var assembly = Assembly.GetExecutingAssembly();
                var methodName = s.GetFrames().Select(f => f.GetMethod()).First(m => m.Module.Assembly == assembly).Name;
                AppSettings = new AppSettings() { TcpConnectionSetting =  new TcpConnectionSetting() };

                Save();
            }
        }

        public static void Save()
        {
            var json = JsonConvert.SerializeObject(AppSettings, Formatting.Indented);
            File.WriteAllText(SettingFile, json);
        }
    }
}
