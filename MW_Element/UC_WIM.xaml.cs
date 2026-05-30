using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WindowsCostumizeWizard.MW_Element.UC_Advansed;
using WindowsCostumizeWizard.Windowinfo;
using WindowsCostumizeWizard.WindowInfo;

namespace WindowsCostumizeWizard.MW_Element
{
    public partial class UC_WIM : UserControl, INotifyPropertyChanged
    {
        private ResourceDictionary RES => System.Windows.Application.Current.Resources;
        public UC_WIM()
        {
            InitializeComponent();
            DataContext = this;

            txtAddresISO.Text = RES["Text_NoMountWim"] as string;
            txtStatusProgresWim.Text = RES["Text_NoMountWim"] as string;

            StatusText = "";
        }

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public void SetIndexWimState(bool enabled)
        {
            cbMountIndexWIM.IsEnabled = enabled;
            cbMountIndexWIM.Opacity = enabled ? 1.0 : 0.5;
            btnMountedWIM.IsEnabled = enabled;            
        }

        private void SetMountedState(bool mounted)
        {
            btnUnmountWimSave.IsEnabled = mounted;
            btnUnmountWimNoSave.IsEnabled = mounted;
        }

        public void LoadWimIndexes()
        {
            string extractPath = Path.Combine(wcwAppState.ExtractIsoPath);
            string wimPath = Path.Combine(extractPath, "sources", "install.wim");

            cbMountIndexWIM.Items.Clear();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = $"/English /Get-WimInfo /WimFile:\"{wimPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    int index = -1;

                    foreach (var line in output.Split(
                        new[] { Environment.NewLine },
                        StringSplitOptions.RemoveEmptyEntries))
                    {
                        string trimmed = line.Trim();

                        if (trimmed.StartsWith("Index :"))
                            index = int.Parse(trimmed.Substring("Index :".Length).Trim());

                        else if (trimmed.StartsWith("Name :") && index != -1)
                        {
                            string name = trimmed.Substring("Name :".Length).Trim();

                            cbMountIndexWIM.Items.Add(
                                new WimIndexItem
                                {
                                    Index = index,
                                    Name = name
                                });

                            index = -1;
                        }
                    }
                }

                if (cbMountIndexWIM.Items.Count > 0)
                    cbMountIndexWIM.SelectedIndex = 0;
            }
            catch
            {
                new WindowMessageInfoError(RES["Text_LoadErrorIndex"] as string)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                }.Show();
            }
            RefreshSelectIndex();
        }

        private void RefreshSelectIndex()
        {
            try
            {
                string mountPath = wcwAppState.MountWimPath;

                bool hasContent = Directory.Exists(mountPath) && Directory.GetDirectories(mountPath).Length > 0;

                if (!hasContent)
                    return;

                SetMountedState(true);
                SetIndexWimState(false);
                StatusText = "100%";
                txtStatusProgresWim.Text = StatusText;
                btnScanImageWindow.IsEnabled = true;
                btnCleanupImageWindow.IsEnabled = true;
                btnCII.IsEnabled = false;
                txtAddresISO.Text = wcwAppState.MountWimPath;
                btnOpenMountWim.IsEnabled = true;

                if (Application.Current.MainWindow is MainWindow mw)
                {
                    mw.ActiveButtonOffline(true);
                    mw.ActivateButtonAdv(true);
                    mw.ActivateButtonDI_ISO(false);
                    mw.btnWcwFolder.IsEnabled = false;
                }

                int mountedIndex = wcwAppState.MountIndex;

                if (mountedIndex <= 0)
                    return;

                for (int i = 0; i < cbMountIndexWIM.Items.Count; i++)
                {
                    var item = cbMountIndexWIM.Items[i] as WimIndexItem;
                    if (item == null)
                        continue;

                    if (item.Index == mountedIndex)
                    {
                        cbMountIndexWIM.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch
            {
            }
        }

        private void OpenMountImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(wcwAppState.MountWimPath))
                return;

            System.Diagnostics.Process.Start("explorer.exe", wcwAppState.MountWimPath);
        }

        private async void MountIndex_Click(object sender, RoutedEventArgs e)
        {
            var blockMount = new WindowProgressBlock(RES["Text_BlockMountWim"] as string)
            {
                Owner = Application.Current.MainWindow
            };

            try
            {
                blockMount.Show();
                Window.GetWindow(this).IsEnabled = false;

                if (cbMountIndexWIM.SelectedItem == null) return;
                var item = (WimIndexItem)cbMountIndexWIM.SelectedItem;
                string extractPath = Path.Combine(wcwAppState.ExtractIsoPath);
                string wimPath = Path.Combine(extractPath, "sources", "install.wim");
                string mountDir = Path.Combine(wcwAppState.MountWimPath);
                string dismCommand = $"dism /English /Mount-Wim /WimFile:\"{wimPath}\" /index:{item.Index} /MountDir:\"{mountDir}\"";
                bool success = await RunProcessWithProgress("cmd.exe", dismCommand);

                if (success)
                {
                    SetMountedState(true);
                    StatusText = "100%";
                    txtStatusProgresWim.Text = StatusText;
                    btnScanImageWindow.IsEnabled = true;
                    btnCleanupImageWindow.IsEnabled = true;
                    SetIndexWimState(false);
                    txtAddresISO.Text = wcwAppState.MountWimPath;
                    btnOpenMountWim.IsEnabled = true;
                    btnCII.IsEnabled = false;
                }
                else
                {
                    StatusText = "Mount failed";
                    txtStatusProgresWim.Text = StatusText;
                }

                blockMount.Close();
                if (Application.Current.MainWindow is MainWindow mw)
                {
                    mw.ActiveButtonOffline(true);
                    mw.ActivateButtonAdv(true);
                    mw.ActivateButtonDI_ISO(false);
                    mw.btnWcwFolder.IsEnabled = false;
                }
            }
            finally
            {
                blockMount.Close();
                Window.GetWindow(this).IsEnabled = true;
            }
        }

        private async Task<bool> RunProcessWithProgress(string exe, string args)
        {
            var tcs = new TaskCompletionSource<bool>();

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "/c " + args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var process = new Process
            {
                StartInfo = psi
            };

            var fullOutput = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    fullOutput.AppendLine(e.Data);
                    ParseProgress(e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    fullOutput.AppendLine(e.Data);
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                bool success = process.ExitCode == 0;

                string[] lines = fullOutput.ToString()
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                var filtered = new StringBuilder();
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();

                    if (trimmed.Contains("[") && trimmed.Contains("]"))
                        continue;

                    if (trimmed.StartsWith("Error:") ||
                        (!trimmed.StartsWith("Deployment") &&
                         !trimmed.StartsWith("Microsoft") &&
                         !trimmed.StartsWith("Version:") &&
                         !trimmed.StartsWith("C:\\") &&
                         !string.IsNullOrWhiteSpace(trimmed)))
                    {
                        filtered.AppendLine(trimmed);
                    }
                }

                string endMessage = filtered.Length == 0
                    ? "The operation completed successfully."
                    : filtered.ToString().Trim();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var winEnd = new W_WindowEndMount
                    {
                        Owner = Application.Current.MainWindow,
                        InfoEndMount = endMessage,
                        Topmost = false,
                        ShowInTaskbar = false
                    };

                    winEnd.Show();
                    Application.Current.MainWindow.Activate();
                });

                process.Dispose();
                return success;
            }
            catch
            {
                return false;
            }
        }

        private void ParseProgress(string line)
        {
            var match = Regex.Match(line, @"(\d+(\.\d+)?)%");

            if (match.Success)
            {
                string percent = match.Groups[1].Value;

                Dispatcher.Invoke(() =>
                {
                    StatusText = $"Прогрес {percent}%...";
                    txtStatusProgresWim.Text = StatusText;
                });
            }
        }

        private void UnmountSave_Click(object sender, RoutedEventArgs e) => UnmountWim(true);

        private void UnmountNoSave_Click(object sender, RoutedEventArgs e) => UnmountWim(false);

        private async void UnmountWim(bool save)
        {
            var blockMount = new WindowProgressBlock(RES["Text_BlockUnmountWim"] as string)
            {
                Owner = Application.Current.MainWindow
            };

            try
            {
                blockMount.Show();
                Window.GetWindow(this).IsEnabled = false;

                string mountDir = wcwAppState.MountWimPath;
                string arg = save ? "/Commit" : "/Discard";
                string cmdArgs = $"dism /English /Unmount-Wim /MountDir:\"{mountDir}\" {arg}";
                bool success = await RunProcessWithProgress("cmd.exe", cmdArgs);

                if (success)
                {
                    try
                    {
                        string tempAdiPath = Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            "temp_adi.txt");

                        if (File.Exists(tempAdiPath))
                        {
                            File.SetAttributes(tempAdiPath, FileAttributes.Normal);
                            File.Delete(tempAdiPath);
                        }
                    }
                    catch
                    {
                    }

                    SetMountedState(false);
                    btnScanImageWindow.IsEnabled = false;
                    btnCleanupImageWindow.IsEnabled = false;
                    StatusText = save ? RES["Text_UnmountDiscard"] as string : RES["Text_UnmountCommit"] as string;
                    btnOpenMountWim.IsEnabled = false;
                    txtAddresISO.Text = RES["Text_NoMountWim"] as string;

                    if (Application.Current.MainWindow is MainWindow mw)
                    {
                        mw.ActiveButtonOffline(false);
                        mw.ActivateButtonAdv(false);
                        mw.CheckStatusIsoState();
                        mw.btnWcwFolder.IsEnabled = true;
                    }
                }
                else
                {
                    StatusText = "Unmount failed";
                }
            }
            finally
            {
                txtStatusProgresWim.Text = StatusText;
                blockMount.Close();
                Window.GetWindow(this).IsEnabled = true;

                if (Application.Current.MainWindow is MainWindow mw)
                {
                    mw.RefreshFeaturesOnline();
                }
            }
        }

        private async void Cleanup_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(wcwAppState.MountWimPath) || !Directory.Exists(wcwAppState.MountWimPath))
            {
                MessageBox.Show(RES["TextErrorMountWIM"] as string);
                return;
            }

            var block = new WindowProgressBlock(RES["Text_Cleanup"] as string)
            {
                Owner = Application.Current.MainWindow
            };
            block.Show();

            string args = $"/English /Cleanup-Image /Image:\"{wcwAppState.MountWimPath}\" /StartComponentCleanup";

            bool success = await RunProcessWithProgress("dism.exe", args);

            StatusText = success ? RES["Text_СComplete"] as string : RES["Text_CFailed"] as string;
            txtStatusProgresWim.Text = StatusText;

            block.Close();
        }

        private void ScanImageWindow_Click(object sender, RoutedEventArgs e)
        {
            var win = new W_WindowScanImage
            {
                Owner = Application.Current.MainWindow
            };

            win.ShowDialog();
        }

        public void SetOscdimgState(bool enabled)
        {
            TextStatusOscdimg.Text = enabled
                ? "oscdimg ✔️"
                : "oscdimg ❌";
        }

        private void CreateImageIso_Click(object sender, RoutedEventArgs e)
        {
            var wcii = new W_CreateImageIso
            {
                Owner = Window.GetWindow(this)
            };

            wcii.ShowDialog();
        }

        private void AllInfoOscdimg_Click(object sender, RoutedEventArgs e)
        {
            string text = Application.Current.Resources["Text_AllInfoOscdimg"] as string;

            var infoWindow = new WindowMessageInfo(text);
            infoWindow.Owner = Window.GetWindow(this);
            infoWindow.ShowDialog();
        }
    }
}