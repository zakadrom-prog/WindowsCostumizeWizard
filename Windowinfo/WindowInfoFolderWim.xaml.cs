using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WindowsCostumizeWizard.Windowinfo
{
    public partial class WindowInfoFolderWim : Window
    {
        public string InfoText { get; set; }

        public WindowInfoFolderWim(string text = "")
        {
            InitializeComponent();
            InfoText = text.Replace("\\n", "\n");
            DataContext = this;
            Loaded += WindowInfoFolderWim_Loaded;
        }

        private async void WindowInfoFolderWim_Loaded(object sender, RoutedEventArgs e)
        {
            await ImageValidation();
        }

        private async Task ImageValidation()
        {
            try
            {
                this.IsEnabled = false;

                string mountPath = wcwAppState.MountWimPath;

                bool imageOk = await Task.Run(() =>
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "dism.exe",
                            Arguments = "/English /Get-MountedWimInfo",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8,
                            StandardErrorEncoding = Encoding.UTF8
                        };

                        using (Process process = Process.Start(psi))
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();

                            if (string.IsNullOrWhiteSpace(output))
                                return false;

                            string[] blocks = output.Split(new[]
                            {
                        "Mount Dir :"
                    }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string rawBlock in blocks)
                            {
                                string block = "Mount Dir :" + rawBlock;

                                if (!block.Contains(mountPath))
                                    continue;

                                // 🔥 1. ВИТЯГУЄМО STATUS
                                string statusLine = block
                                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                    .FirstOrDefault(x =>
                                        x.Trim().StartsWith("Status", StringComparison.OrdinalIgnoreCase));

                                // 🔥 2. ВИТЯГУЄМО INDEX (ОЦЕ ТЕ ЩО ТИ ХОТІВ)
                                string indexLine = block
                                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                    .FirstOrDefault(x =>
                                        x.Trim().StartsWith("Image Index", StringComparison.OrdinalIgnoreCase) ||
                                        x.Trim().StartsWith("Index", StringComparison.OrdinalIgnoreCase));

                                if (indexLine != null)
                                {
                                    string indexValue = indexLine.Split(':').Last().Trim();

                                    if (int.TryParse(indexValue, out int index))
                                    {
                                        wcwAppState.MountIndex = index; // 🔥 ОЦЕ ГОЛОВНЕ
                                    }
                                }

                                if (statusLine != null)
                                {
                                    string statusValue = statusLine
                                        .Split(':')
                                        .Last()
                                        .Trim();

                                    if (statusValue.Equals("Ok", StringComparison.OrdinalIgnoreCase) ||
                                        statusValue.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                                    {
                                        return true;
                                    }
                                }

                                return false;
                            }

                            return false;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (imageOk)
                {
                    btnOpen.IsEnabled = true;
                    btnCheck.IsEnabled = false;
                }
                else
                {
                    btnOpen.IsEnabled = false;
                    btnCheck.IsEnabled = true;
                }
            }
            finally
            {
                this.IsEnabled = true;
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OpenProgram_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void ImageCheck_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MountValidation.exe");

                if (!File.Exists(exePath))
                {
                    MessageBox.Show("MountValidation.exe not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Process.Start(exePath);

                Application.Current.Shutdown();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}