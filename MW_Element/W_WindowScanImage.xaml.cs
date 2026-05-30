using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using WindowsCostumizeWizard.WindowInfo;

namespace WindowsCostumizeWizard.MW_Element
{
    public partial class W_WindowScanImage : Window
    {
        public W_WindowScanImage()
        {
            InitializeComponent();
        }

        private async void ScanPerform_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(wcwAppState.MountWimPath) || !Directory.Exists(wcwAppState.MountWimPath))
            {
                MessageBox.Show("MountWIM path not set or does not exist.");
                return;
            }

            string args = "";

            if (rbCheckHealth.IsChecked == true)
                args = $"/Image:\"{wcwAppState.MountWimPath}\" /Cleanup-Image /CheckHealth";

            if (rbScanHealth.IsChecked == true)
                args = $"/Image:\"{wcwAppState.MountWimPath}\" /Cleanup-Image /ScanHealth";

            var block = new WindowProgressBlock("Scanning Component Store...")
            {
                Owner = Application.Current.MainWindow
            };

            block.Show();

            bool success = await RunProcessWithScan("dism.exe", args);

            if (!success)
                txtStatusProgresScan.Text = "Scan failed";

            block.Close();
        }

        private Task<bool> RunProcessWithScan(string exe, string args)
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

            var process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            string percent = "";
            string result = "";

            process.OutputDataReceived += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data)) return;

                ParseScanLine(e.Data, ref percent, ref result);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data)) return;

                ParseScanLine(e.Data, ref percent, ref result);
            };

            process.Exited += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (rbCheckHealth.IsChecked == true)
                        txtStatusProgresScan.Text = result;

                    if (rbScanHealth.IsChecked == true)
                        txtStatusProgresScan.Text = percent + " " + result;
                });

                tcs.SetResult(process.ExitCode == 0);
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }

        private void ParseScanLine(string line, ref string percent, ref string result)
        {
            var match = Regex.Match(line, @"(\d+(\.\d+)?)%");

            if (match.Success)
                percent = match.Groups[1].Value + "%";

            if (line.Contains("No component store corruption detected"))
                result = "No component store corruption detected.";
        }

        private void WindowScanClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}