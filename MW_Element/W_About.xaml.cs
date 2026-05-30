using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace WindowsCostumizeWizard.MW_Element
{
    public partial class W_About : Window
    {
        private ResourceDictionary RES => System.Windows.Application.Current.Resources;
        public string InfoAbout { set; get; }
        public W_About(string text = "")
        {
            InitializeComponent();
            InfoAbout = text.Replace("\\n", "\n");
            DataContext = this;
            LoadVersion();
            UpdateButtonsState();
            CheckUpdates();
        }


        private async void CheckUpdates()
        {
            string url = "https://github.com/zakadrom-prog/WindowsCostumizeWizard/releases/download/v1.0.0.0/update.json";

            string jsonText;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    jsonText = await client.GetStringAsync(url);
                }
            }
            catch
            {
                txtProgresUpdateInfo.Text = RES["txt_NoJson"] as string;
                return;
            }

            JObject jsonObj;

            try
            {
                jsonObj = JObject.Parse(jsonText);
            }
            catch
            {
                txtProgresUpdateInfo.Text = RES["txt_NoJsonRead"] as string;
                return;
            }

            string versionStr = jsonObj["version"]?.ToString();
            string sizeStr = jsonObj["size"]?.ToString();

            if (string.IsNullOrWhiteSpace(versionStr))
            {
                txtProgresUpdateInfo.Text = RES["txt_NoJsonRead"] as string;
                return;
            }

            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            if (!Version.TryParse(versionStr, out Version newVersion))
            {
                txtProgresUpdateInfo.Text = RES["txt_NoJsonRead"] as string;
                return;
            }

            if (newVersion > currentVersion)
            {
                txtProgresUpdateInfo.Text = $"{RES["txt_NewUpdate"]} {currentVersion} → {newVersion} | {sizeStr}";

                txtLinkUpdate.IsEnabled = true;
                txtLinkUpdate.Opacity = 1.0;
            }
            else
            {
                txtProgresUpdateInfo.Text = RES["txt_NoUpdate"] as string;

                txtLinkUpdate.IsEnabled = false;
                txtLinkUpdate.Opacity = 0.5;
            }
        }

        private void LoadVersion()
        {
            string version = Assembly.GetExecutingAssembly()
                                     .GetName()
                                     .Version
                                     .ToString();

            txtVersion.Text = version;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });

            e.Handled = true;
        }

        private void UpdateButtonsState()
        {
            AppConfig config = AppConfig.Load();

            if (config.WindowUpdate)
            {
                btnEnableUpdates.IsEnabled = false;
                btnDisableUpdates.IsEnabled = true;
            }
            else
            {
                btnEnableUpdates.IsEnabled = true;
                btnDisableUpdates.IsEnabled = false;
            }
        }

        private void DisableUpdates_Click(object sender, RoutedEventArgs e)
        {
            AppConfig config = AppConfig.Load();
            config.WindowUpdate = false;
            config.Save();
            UpdateButtonsState();
        }

        private void EnableUpdates_Click(object sender, RoutedEventArgs e)
        {
            AppConfig config = AppConfig.Load();
            config.WindowUpdate = true;
            config.Save();
            UpdateButtonsState();
        }

        private void OpenUpdate_Click(object sender, RoutedEventArgs e)
        {
            string updatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update.exe");

            if (File.Exists(updatePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = updatePath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("Update.exe не знайдено");
            }

            this.Close();
        }

        private void CloseWindowAbout_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}
