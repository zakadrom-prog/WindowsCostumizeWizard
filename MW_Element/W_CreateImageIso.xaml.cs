using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WindowsCostumizeWizard.MW_Element
{
    public partial class W_CreateImageIso : Window
    {
        private string _selectedIsoFolder;
        private string _oscdimgPath;

        public W_CreateImageIso()
        {
            InitializeComponent();

            AddressEndIsoPath.Text = "(не вибрано)";
            AddressExtractIsoPath.Text = wcwAppState.ExtractIsoPath;
            _oscdimgPath = GetOscdimgPath();
            btnCreateIso.IsEnabled = false;
        }

        // =========================
        // SELECT FOLDER
        // =========================
        private void OpenFolderIso_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            string selectedPath = dlg.FileName;

            _selectedIsoFolder = Path.Combine(selectedPath, "wcw_My_iso");
            Directory.CreateDirectory(_selectedIsoFolder);

            AddressEndIsoPath.Text = _selectedIsoFolder;

            btnCreateIso.IsEnabled = true;
        }

        // =========================
        // CREATE ISO
        // =========================
        private async void CreateIso_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;

            try
            {
                await Task.Run(() =>
                    RunOscdimg(_oscdimgPath, _selectedIsoFolder));
            }
            finally
            {
                this.IsEnabled = true;
            }
        }

        // =========================
        // OSCDIMG PATH
        // =========================
        private string GetOscdimgPath()
        {
            if (wcwAppState.OscdimgSysPath)
            {
                return @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\x86\Oscdimg\oscdimg.exe";
            }

            if (wcwAppState.OscdimgProgPath)
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oscdimg", "oscdimg.exe");
            }

            return null;
        }

        // =========================
        // RUN REAL CONSOLE (CMD)
        // =========================
        private void RunOscdimg(string exePath, string outputFolder)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                MessageBox.Show("oscdimg.exe not found");
                return;
            }

            string sourcePath = wcwAppState.ExtractIsoPath;
            string bootFile = Path.Combine(sourcePath, "boot", "etfsboot.com");
            string outputIso = Path.Combine(outputFolder, "windows_custom.iso");

            string arguments =
                $"/k \"\"{exePath}\" -m -o -u2 -udfver102 " +
                $"-b\"{bootFile}\" " +
                $"\"{sourcePath}\" " +
                $"\"{outputIso}\"\"";

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = arguments,

                UseShellExecute = true,
                CreateNoWindow = false
            };

            Process.Start(psi);
        }

        // =========================
        // WINDOW EVENTS
        // =========================
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}