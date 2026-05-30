using System;
using System.IO;
using Newtonsoft.Json;

namespace WindowsCostumizeWizard
{
    public class AppConfig
    {
        public double ScreenWidth { get; set; }
        public double ScreenHeight { get; set; }
        public string Language { get; set; } = "ua";
        public string Theme { get; set; } = "Dark";
        public string WorkDirectory { get; set; }
        public bool HasWimlib { get; set; } = false;
        public bool WasRunAsAdmin { get; set; } = false;
        public bool OscdimgPath { get; set; } = false;
        public string OscdimgExePath { get; set; }
        public bool WindowUpdate { get; set; } = true;

        private static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WCWConfig");

        private static readonly string ConfigFilePath =  Path.Combine(ConfigDirectory, "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                    Directory.CreateDirectory(ConfigDirectory);

                if (!File.Exists(ConfigFilePath))
                {
                    var defaultConfig = new AppConfig();
                    defaultConfig.Save();
                    return defaultConfig;
                }

                string json = File.ReadAllText(ConfigFilePath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                var defaultConfig = new AppConfig();
                defaultConfig.Save();
                return defaultConfig;
            }
        }

        public void Save()
        {
            if (!Directory.Exists(ConfigDirectory))
                Directory.CreateDirectory(ConfigDirectory);

            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}