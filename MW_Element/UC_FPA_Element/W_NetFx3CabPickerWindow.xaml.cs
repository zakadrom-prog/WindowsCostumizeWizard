using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WindowsCostumizeWizard.MW_Element.UC_FPA_Element
{
    public partial class W_NetFx3CabPickerWindow : Window, INotifyPropertyChanged
    {
        private string _nameNetFx3;

        public string NameNetFx3
        {
            get => _nameNetFx3;
            set
            {
                _nameNetFx3 = value;
                OnPropertyChanged();
            }
        }

        private string _progressText;
        public string ProgressText
        {
            get => _progressText;
            set
            {
                _progressText = value;
                OnPropertyChanged();
            }
        }

        public string FullPath { get; set; }

        public W_NetFx3CabPickerWindow()
        {
            InitializeComponent();
            DataContext = this;

            NameNetFx3 = Application.Current.Resources["Text_NoSelect"] as string;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void SelectCab_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CAB files (*.cab)|*.cab",
                Title = Application.Current.Resources["Text_SelectCab"] as string
            };

            if (dialog.ShowDialog() == true)
            {
                FullPath = dialog.FileName;

                NameNetFx3 = Path.GetFileName(FullPath);
                btnInstallCab.IsEnabled = true;
            }
        }

        private string BuildDismCommand()
        {
            if (string.IsNullOrEmpty(FullPath))
                return null;

            return $"dism /online /add-package /packagepath:\"{FullPath}\"";
        }

        private async void InstallCab_Click(object sender, RoutedEventArgs e)
        {
            var command = BuildDismCommand();

            btnSelectCab.IsEnabled = false;
            btnInstallCab.IsEnabled = false;
            btnAllCab.IsEnabled = false;

            try
            {
                var process = new Process();

                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c " + command;

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.StartInfo.Verb = "runas";

                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_OutputDataReceived;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());
            }
            finally
            {
                btnAllCab.IsEnabled = true;
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Data))
                return;

            // шукаємо щось типу [===20.0%===]
            var match = System.Text.RegularExpressions.Regex.Match(e.Data, @"(\d{1,3}(\.\d+)?)%");

            if (match.Success)
            {
                var value = match.Groups[1].Value;

                Dispatcher.Invoke(() =>
                {
                    ProgressText = value + "%";
                });
            }

            // фінал
            if (e.Data.Contains("100.0%") || e.Data.Contains("100%"))
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressText = "100.0%";
                });
            }
        }

        private async void CloseWindowNetFx_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            if (Application.Current.MainWindow is MainWindow mw)
            {
                await mw.RefreshFeatures();
            }
        }
    }
}
