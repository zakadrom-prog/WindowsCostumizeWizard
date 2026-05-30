using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WindowsCostumizeWizard.Windowinfo;
using WindowsCostumizeWizard.WindowInfo;

namespace WindowsCostumizeWizard.MW_Element
{
    public class WimIndexItem
    {
        public int Index { get; set; }
        public string Name { get; set; }

        public override string ToString() => Name;
    }

    public partial class UC_DeleteIndex : UserControl
    {
        public UC_DeleteIndex()
        {
            InitializeComponent();
            TextStatusWim.Text = Application.Current.Resources["Text_NoImage"] as string;
            txtProgress1.Text = Application.Current.Resources["Text_StatusOptimize1"] as string;
        }

        public void SetIndexWimState(bool enabled)
        {
            cbIndexWIM.IsEnabled = enabled;
            cbIndexWIM.Opacity = enabled ? 1.0 : 0.5;
            btnDeleteIndex.IsEnabled = enabled;
        }

        private void ReloadIndexes_Click(object sender, RoutedEventArgs e)
        {
            LoadWimIndexes();
        }

        private void OpenConvert_Click(object sender, RoutedEventArgs e)
        {
            W_Converter win = new W_Converter();
            win.ShowDialog();
        }

        private void DetailInstall_Click(object sender, RoutedEventArgs e)
        {
            W_InfoWimEsd win = new W_InfoWimEsd();
            win.Show();
        }

        public void LoadWimIndexes()
        {
            string extractPath = Path.Combine(wcwAppState.ExtractIsoPath);
            string wimPath = Path.Combine(extractPath, "sources", "install.wim");

            if (!File.Exists(wimPath))
            {
                TextStatusWim.Text = Application.Current.Resources["Text_NoImage"] as string;
                cbIndexWIM.Items.Clear();
                btnDeleteIndex.IsEnabled = false;
                return;
            }

            try
            {
                cbIndexWIM.Items.Clear();

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = $"/Get-WimInfo /WimFile:\"{wimPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    using (StringReader reader = new StringReader(output))
                    {
                        string line;
                        int currentIndex = -1;
                        string currentName = "";

                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();

                            if (line.StartsWith("Index :"))
                            {
                                currentIndex = int.Parse(line.Substring("Index :".Length).Trim());
                            }
                            else if (line.StartsWith("Name :"))
                            {
                                currentName = line.Substring("Name :".Length).Trim();
                            }

                            if (currentIndex != -1 && !string.IsNullOrEmpty(currentName))
                            {
                                cbIndexWIM.Items.Add(new WimIndexItem
                                {
                                    Index = currentIndex,
                                    Name = currentName
                                });

                                currentIndex = -1;
                                currentName = "";
                            }
                        }
                    }
                }

                if (cbIndexWIM.Items.Count > 0)
                    cbIndexWIM.SelectedIndex = 0;

                TextStatusWim.Text = Path.GetFileName(wimPath);
                btnDeleteIndex.IsEnabled = cbIndexWIM.Items.Count > 1;
            }
            catch
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var infoWindow = new WindowMessageInfoError(Application.Current.Resources["Text_LoadErrorIndex"] as string);

                    infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    infoWindow.Show();
                }));

                btnDeleteIndex.IsEnabled = false;
            }
        }

        private void DeleteIndex_Click(object sender, RoutedEventArgs e)
        {
            if (cbIndexWIM.Items.Count <= 1)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var infoWindow = new WindowMessageInfoError(Application.Current.Resources["Text_NoDeleteIndex"] as string);

                    infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    infoWindow.Show();
                }));

                btnDeleteIndex.IsEnabled = false;
                return;
            }

            if (cbIndexWIM.SelectedItem == null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var infoWindow = new WindowMessageInfoError(Application.Current.Resources["Text_NoSelectIndex"] as string);

                    infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    infoWindow.Show();
                }));
                return;
            }

            var selectedItem = cbIndexWIM.SelectedItem as WimIndexItem;

            try
            {
                string extractPath = Path.Combine(App.Config.WorkDirectory, "ExtractISO");
                string wimPath = Path.Combine(extractPath, "sources", "install.wim");

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = $"/Delete-Image /ImageFile:\"{wimPath}\" /Index:{selectedItem.Index} /Quiet",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using (Process proc = Process.Start(psi))
                {
                    proc.WaitForExit();
                }

                LoadWimIndexes();

                if (Application.Current.MainWindow is MainWindow mw)
                {
                    mw.RefreshIndexUCWim();
                }
            }
            catch
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var infoWindow = new WindowMessageInfoError(Application.Current.Resources["Text_ErrorDeleteIndex"] as string);

                    infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    infoWindow.Show();
                }));
            }
        }
        public void SetWimlibState(bool enabled)
        {
            TextStatusWimlib.Text = enabled
                ? "wimlib ✔️"
                : "wimlib ❌";
        }

        private void OptimizeWim_Click(object sender, RoutedEventArgs e)
        {
            btnOptimizeImage.IsEnabled = false;
            btnUpdateIndex.IsEnabled = false;
            btnDeleteIndex.IsEnabled = false;
            btnConvertImage.IsEnabled = false;
            txtProgress1.Text = "0%";

            string extractPath = Path.Combine(App.Config.WorkDirectory, "ExtractISO");
            string wimFile = Path.Combine(extractPath, "sources", "install.wim");
            string wimlibPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wimlib", "wimlib-imagex.exe");

            if (!File.Exists(wimlibPath) || !File.Exists(wimFile))
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var infoWindow = new WindowMessageInfoError(Application.Current.Resources["Text_ErrorWimlibInfo"] as string);

                    infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    infoWindow.Show();
                }));

                btnOptimizeImage.IsEnabled = true;
                btnUpdateIndex.IsEnabled = true;
                btnDeleteIndex.IsEnabled = true;
                btnConvertImage.IsEnabled = true;
                return;
            }

            // Запуск оптимізації в окремому потоці
            Task.Run(() => OptimizeWim(wimlibPath, wimFile));
        }

        private void OptimizeWim(string wimlibExe, string wimFile)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = wimlibExe,
                    Arguments = $"optimize \"{wimFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = psi;

                    process.OutputDataReceived += (s, e) =>
                    {
                        if (string.IsNullOrEmpty(e.Data)) return;

                        // Ловимо відсоток у рядку, наприклад "(100%)"
                        var match = Regex.Match(e.Data, @"\((\d+)%\)");
                        if (match.Success)
                        {
                            int percent = int.Parse(match.Groups[1].Value);
                            Dispatcher.Invoke(() =>
                                txtProgress1.Text = $"{percent}%");
                        }
                        else if (e.Data.Contains("done"))
                        {
                            Dispatcher.Invoke(() =>
                                txtProgress1.Text = "100%");
                        }
                    };

                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Dispatcher.Invoke(() =>
                                txtProgress1.Text = (Application.Current.Resources["Text_ErrorTitle"] as string) + " " + e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    // Після завершення включаємо всі кнопки
                    Dispatcher.Invoke(() =>
                    {
                        btnOptimizeImage.IsEnabled = true;
                        btnUpdateIndex.IsEnabled = true;
                        btnConvertImage.IsEnabled = true;
                        LoadWimIndexes();

                    });
                }
            }

            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    txtProgress1.Text = (Application.Current.Resources["Text_ErrorTitle"] as string) + " " + ex.Message;
                    btnOptimizeImage.IsEnabled = true;
                });
            }
        }

        private void AllInfoWL_Click(object sender, RoutedEventArgs e)
        {
            string text = Application.Current.Resources["Text_AllInfo"] as string;

            var infoWindow = new WindowMessageInfo(text);
            infoWindow.Owner = Window.GetWindow(this);
            infoWindow.ShowDialog();
        }
    }
}