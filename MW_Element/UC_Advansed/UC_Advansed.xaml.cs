using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using WindowsCostumizeWizard.MW_Element.UC_FPA_Element;
using WindowsCostumizeWizard.WindowInfo;

namespace WindowsCostumizeWizard.MW_Element.UC_Advansed
{
    public partial class UC_Advansed : UserControl
    {
        private int _lastCurrent = -1;
        private int _lastTotal = -1;

        private static readonly Regex ProgressRegex =
            new Regex(@"Exporting\s+(\d+)\s+of\s+(\d+)", RegexOptions.Compiled);

        public UC_Advansed()
        {
            InitializeComponent();
            ProgressDriversText.Text = Application.Current.Resources["Text_DriversNoInfo"] as string;
        }

        private async void GetDrivers_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}"
            };

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            string selectedPath = dialog.FileName;
            string backupPath = Path.Combine(selectedPath, "WCW_DriverBackup");

            Directory.CreateDirectory(backupPath);

            _lastCurrent = -1;
            _lastTotal = -1;

            var owner = Window.GetWindow(this);

            var blockGetDrivers = new WindowProgressBlock(Application.Current.Resources["Text_BlockGetDrivers"] as string)
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            owner.IsEnabled = false;
            blockGetDrivers.Show();

            try
            {
                await RunDismAsync(backupPath);
            }
            finally
            {
                blockGetDrivers.Close();
                owner.IsEnabled = true;
            }
        }

        private Task RunDismAsync(string backupPath)
        {
            return Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = $"/online /export-driver /destination:\"{backupPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();

                    var reader = process.StandardOutput;

                    char[] buffer = new char[1];
                    string line = "";

                    while (!reader.EndOfStream)
                    {
                        int read = reader.Read(buffer, 0, 1);
                        if (read <= 0) break;

                        char c = buffer[0];

                        if (c == '\r' || c == '\n')
                        {
                            ParseProgress(line);
                            line = "";
                        }
                        else
                        {
                            line += c;

                            ParseProgress(line);
                        }
                    }

                    process.WaitForExit();
                }
            });
        }

        private void ParseProgress(string text)
        {
            var match = ProgressRegex.Match(text);

            if (!match.Success)
                return;

            int current = int.Parse(match.Groups[1].Value);
            int total = int.Parse(match.Groups[2].Value);

            if (current == _lastCurrent && total == _lastTotal)
                return;

            _lastCurrent = current;
            _lastTotal = total;

            Dispatcher.Invoke(() =>
            {
                ProgressDriversText.Text = $"Exporting {current} of {total}";
            });
        }

        private void AddDriversSystem_Click(object sender, RoutedEventArgs e)
        {
            var wads = new W_AddDriversSystem
            {
                Owner = Window.GetWindow(this)
            };

            wads.ShowDialog();
        }

        private void AddDriversImage_Click(object sender, RoutedEventArgs e)
        {
            var wadi = new W_AddDriversImage
            {
                Owner = Window.GetWindow(this)
            };

            wadi.ShowDialog();
        }

        private async void DISMRestoreHealth_Click(object sender, RoutedEventArgs e)
        {
            await RunCommandWithUiLockAsync("DISM /Online /Cleanup-Image /RestoreHealth");
        }

        private async void DismCleanup_Click(object sender, RoutedEventArgs e)
        {
            await RunCommandWithUiLockAsync("DISM /Online /Cleanup-Image /StartComponentCleanup");
        }

        private async Task RunCommandWithUiLockAsync(string command)
        {
            var window = Window.GetWindow(this);
            window.IsEnabled = false;

            try
            {
                await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        UseShellExecute = true,   // ОБОВ'ЯЗКОВО для видимої консолі
                        CreateNoWindow = false     // показати вікно
                    };

                    using (var process = Process.Start(psi))
                    {
                        process.WaitForExit();
                    }
                });
            }
            finally
            {
                window.IsEnabled = true;
            }
        }

        private void OpenFCIW_Click(object sender, RoutedEventArgs e)
        {
            btnFCIW.IsEnabled = false;
            var infohelp = new W_SelectFolderCreateWim(Application.Current.Resources["Text_SelFolderCreateWIM"] as string);
            infohelp.Owner = Window.GetWindow(this);
            infohelp.Show();
        }
    }
}