using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace WindowsCostumizeWizard.MW_Element
{
    public partial class W_About : Window
    {
        private ResourceDictionary RES => System.Windows.Application.Current.Resources;

        public string InfoAbout { get; set; }

        public W_About(string text = "")
        {
            InitializeComponent();

            InfoAbout = text.Replace("\\n", "\n");
            DataContext = this;

            LoadVersion();
            LoadUpdateSettings();
        }

        private void LoadUpdateSettings()
        {
            AppConfig config = AppConfig.Load();

            rbEnableUpdate.IsChecked = config.WindowUpdate;
            rbDisableUpdate.IsChecked = !config.WindowUpdate;

            if (config.WindowUpdate)
            {
                CheckUpdates();
            }
            else
            {
                txtProgresUpdateInfo.Text = RES["txt_NoCheckUpdate"] as string;
            }
        }

        private async void CheckUpdates()
        {
            string url = "https://raw.githubusercontent.com/zakadrom-prog/WindowsCostumizeWizard/main/update.json";

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
            string sizeStr = jsonObj["sizezip"]?.ToString();

            if (string.IsNullOrWhiteSpace(versionStr))
            {
                txtProgresUpdateInfo.Text = RES["txt_NoJsonRead"] as string;

                return;
            }

            Version currentVersion = Assembly.GetExecutingAssembly() .GetName() .Version;

            if (!Version.TryParse(versionStr, out Version newVersion))
            {
                txtProgresUpdateInfo.Text = RES["txt_NoJsonRead"] as string;

                return;
            }

            if (newVersion > currentVersion)
            {
                txtProgresUpdateInfo.Text = $"{RES["txt_NewUpdate"]} " + $"{currentVersion} → {newVersion} | {sizeStr}";

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
            string version = Assembly.GetExecutingAssembly() .GetName() .Version .ToString();

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

        private void EnableUpdates_Click(object sender, RoutedEventArgs e)
        {
            AppConfig config = AppConfig.Load();

            config.WindowUpdate = true;
            config.Save();

            rbEnableUpdate.IsChecked = true;
            rbDisableUpdate.IsChecked = false;

            CheckUpdates();
        }

        private void DisableUpdates_Click(object sender, RoutedEventArgs e)
        {
            AppConfig config = AppConfig.Load();

            config.WindowUpdate = false;
            config.Save();

            rbEnableUpdate.IsChecked = false;
            rbDisableUpdate.IsChecked = true;

            txtProgresUpdateInfo.Text = RES["txt_NoCheckUpdate"] as string;

            txtLinkUpdate.IsEnabled = false;
            txtLinkUpdate.Opacity = 0.5;
        }

        private void OpenUpdate_Click(object sender, RoutedEventArgs e)
        {
            string updatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wcw_update.exe");

            if (!File.Exists(updatePath))
            {
                MessageBox.Show(RES["Text_WarningMessage"] as string, RES["Text_WarningMessage"] as string,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning); 

                return;
            }

            // Підтвердження перед завершенням програми.
            MessageBoxResult result = MessageBox.Show(RES["Text_WarningMessageStartUpdate"] as string, RES["Text_WarningMessage"] as string,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = updatePath,
                    UseShellExecute = true
                });

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{RES["Text_NoStartUpdate"] as string}\n\n{ex.Message}", RES["Text_ErrorTitle"] as string,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CloseWindowAbout_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}