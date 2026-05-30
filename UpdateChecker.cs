using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Reflection;
using System.Windows;

namespace WindowsCostumizeWizard
{
    public class UpdateChecker
    {
        private const string UpdateUrl = "https://github.com/zakadrom-prog/WindowsCostumizeWizard/releases/download/v1.0.0.0/update.json";

        public void CheckUpdates()
        {
            string json;

            // 1. Завантажуємо JSON напряму в памʼять
            try
            {
                using (WebClient client = new WebClient())
                {
                    json = client.DownloadString(UpdateUrl);
                }
            }
            catch
            {
                // якщо немає інтернету або GitHub недоступний — просто вихід
                return;
            }

            JObject obj;

            // 2. Парсимо JSON
            try
            {
                obj = JObject.Parse(json);
            }
            catch
            {
                return;
            }

            string versionStr = obj["version"]?.ToString();

            if (string.IsNullOrWhiteSpace(versionStr))
                return;

            // 3. Поточна версія програми
            Version current = Assembly.GetExecutingAssembly().GetName().Version;

            if (!Version.TryParse(versionStr, out Version latest))
                return;

            // 4. Перевірка оновлення
            if (latest > current)
            {
                var win = new UpdateInfo
                {
                    Owner = Application.Current.MainWindow,
                    ShowInTaskbar = true
                };

                Application.Current.MainWindow.Closed += (s, e) =>
                {
                    win.Close();
                };

                win.Show();
            }
        }
    }
}