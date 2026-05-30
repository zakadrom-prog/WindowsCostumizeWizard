using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using WindowsCostumizeWizard.WindowInfo;

namespace WindowsCostumizeWizard.MW_Element
{
    public partial class W_Converter : Window
    {
        enum InstallImageType { None, Wim, Esd }
        private InstallImageType _currentImageType = InstallImageType.None;

        private string extractPath;
        private string sourcesPath;
        private string wimPath;
        private string esdPath;

        public W_Converter()
        {
            InitializeComponent();

            ProgressTextConvert.Text =
                Application.Current.Resources["Text_StatusProgress"] as string;

            Loaded += (s, e) => CheckInstallImage();

            bool hasWimlib = wcwAppState.WimlibExists;

            rbConvertWimlib.IsEnabled = hasWimlib;
            rbConvertWimlib.Opacity = hasWimlib ? 1.0 : 0.5;

            btnConvertImage.Click += ConvertImage_Click;
        }

        private void SetRBActive(bool enabled)
        {
            rbConvertDISM.IsEnabled = enabled;
            rbConvertDISM.Opacity = enabled ? 1.0 : 0.5;
            rbConvertWimlib.IsEnabled = enabled;
            rbConvertWimlib.Opacity = enabled ? 1.0 : 0.5;
        }

        private void CheckInstallImage()
        {
            extractPath = Path.Combine(App.Config.WorkDirectory, "ExtractISO");
            sourcesPath = Path.Combine(extractPath, "sources");
            wimPath = Path.Combine(sourcesPath, "install.wim");
            esdPath = Path.Combine(sourcesPath, "install.esd");

            if (File.Exists(wimPath))
            {
                _currentImageType = InstallImageType.Wim;
                StatusImageInstall.Text = Application.Current.Resources["Text_StatusImageInstall0"] as string;
                btnConvertImage.Content = Application.Current.Resources["Text_ConvertEsd"] as string;
            }
            else if (File.Exists(esdPath))
            {
                _currentImageType = InstallImageType.Esd;
                StatusImageInstall.Text = Application.Current.Resources["Text_StatusImageInstall1"] as string;
                btnConvertImage.Content = Application.Current.Resources["Text_ConvertWim"] as string;
            }
            else
            {
                _currentImageType = InstallImageType.None;
                StatusImageInstall.Text = Application.Current.Resources["Text_NoInstall"] as string;
                btnConvertImage.IsEnabled = false;
            }
        }

        private async void ConvertImage_Click(object sender, RoutedEventArgs e)
        {
            btnConvertImage.IsEnabled = false;
            rbConvertWimlib.IsEnabled = false;
            SetRBActive(false);
            ProgressBarConvert.Value = 0;
            ProgressTextConvert.Text = "";

            bool success = false;

            if (_currentImageType == InstallImageType.Wim)
            {
                success = await ConvertWimToEsd();

                if (success && File.Exists(wimPath))
                    File.Delete(wimPath);
            }
            else if (_currentImageType == InstallImageType.Esd)
            {
                success = await ConvertEsdToWim();

                if (success && File.Exists(esdPath))
                    File.Delete(esdPath);
            }

            if (!success)
            {
                MessageBox.Show(
                    Application.Current.Resources["Text_ErrorConvertImage"] as string,
                    Application.Current.Resources["Text_ErrorTitle"] as string,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            CheckInstallImage();
            SetRBActive(true);
            btnConvertImage.IsEnabled = _currentImageType != InstallImageType.None;

            // ✅ ЄДИНЕ місце роботи з MainWindow
            if (Application.Current.MainWindow is MainWindow mw)
            {
                if (_currentImageType == InstallImageType.Esd)
                {
                    mw.WD_UpdateButtonState();   // для ESD
                }
                else if (_currentImageType == InstallImageType.Wim)
                {
                    mw.CheckStatusIsoState();    // для WIM
                }
            }
        }

        private async Task<bool> ConvertWimToEsd()
        {
            bool isDism = rbConvertDISM.IsChecked == true;
            string exe, args;

            if (isDism)
            {
                exe = "dism.exe";
                args = $"/Export-Image /SourceImageFile:\"{wimPath}\" /SourceIndex:1 /DestinationImageFile:\"{esdPath}\" /Compress:recovery";
            }
            else
            {
                exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wimlib", "wimlib-imagex.exe");
                args = $"export \"{wimPath}\" all \"{esdPath}\" --compress=LZMS --solid";
            }

            return await RunProcessWithProgress(exe, args, isDism);
        }

        private async Task<bool> ConvertEsdToWim()
        {
            bool isDism = rbConvertDISM.IsChecked == true;
            string exe, args;

            if (isDism)
            {
                exe = "dism.exe";
                args = $"/Export-Image /SourceImageFile:\"{esdPath}\" /SourceIndex:1 /DestinationImageFile:\"{wimPath}\" /Compress:max";
            }
            else
            {
                exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wimlib", "wimlib-imagex.exe");
                args = $"export \"{esdPath}\" all \"{wimPath}\" --compress=LZMS --solid";
            }

            return await RunProcessWithProgress(exe, args, isDism);
        }

        private Task<bool> RunProcessWithProgress(string exe, string args, bool isDism)
        {
            var tcs = new TaskCompletionSource<bool>();

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    ParseProgress(e.Data, isDism);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    Console.WriteLine(e.Data);
            };

            process.Exited += (s, e) =>
            {
                bool success = process.ExitCode == 0; // 0 = успіх
                tcs.SetResult(success);
                process.Dispose();
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var infoWindow = new WindowMessageInfo(
                        Application.Current.Resources["Text_ErrorStartProcess"] as string
                    );

                    infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    infoWindow.Show();
                }));
                tcs.SetResult(false);
            }

            return tcs.Task;
        }

        private void ParseProgress(string line, bool isDism)
        {
            Dispatcher.Invoke(() =>
            {
                double progress = -1;

                if (isDism)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+(\.\d+)?)%");
                    if (match.Success)
                        progress = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                }
                else
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"\((\d+)%\)");
                    if (match.Success)
                        progress = double.Parse(match.Groups[1].Value);
                }

                if (progress >= 0)
                {
                    ProgressBarConvert.Value = progress;
                    ProgressTextConvert.Text = $"{progress}%";
                }
            });
        }
    }
}