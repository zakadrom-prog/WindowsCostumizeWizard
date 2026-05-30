using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowsCostumizeWizard.Windowinfo;
using WindowsCostumizeWizard.WindowInfo;

namespace WindowsCostumizeWizard.MW_Element.UC_Advansed
{
    public partial class W_AddDriversImage : Window
    {
        private string _selectedFolder = string.Empty;
        private bool _logColorToggle = false;
        private int _logIndex = 0;
        public class DriverLogItem
        {
            public string Header { get; set; }
            public string Text { get; set; }
        }

        private readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_adi.txt");
        private ResourceDictionary RES => System.Windows.Application.Current.Resources;

        public W_AddDriversImage()
        {
            InitializeComponent();
            TextAddressINF.Text = RES["Text_NoSelect"] as string;
            LoadLogsFromTxt();
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = RES["Text_SelectINF_System"] as string
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string folder = dialog.FileName;

                var infFiles = Directory.GetFiles(folder, "*.inf", SearchOption.AllDirectories);

                if (infFiles.Any())
                {
                    _selectedFolder = folder;
                    TextAddressINF.Text = _selectedFolder;
                    btnAddDrivers.IsEnabled = true;
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var infoWindow = new WindowMessageInfoError(RES["Text_NoInf"] as string);
                        infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        infoWindow.Show();
                    }));
                }
            }
        }

        private async void AddDrivers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_selectedFolder) || !Directory.Exists(_selectedFolder))
                    return;

                if (string.IsNullOrWhiteSpace(wcwAppState.MountWimPath) || !Directory.Exists(wcwAppState.MountWimPath))
                    return;

                Window owner = this;

                var blockAddDrivers = new WindowProgressBlock(RES["Text_BlockAddDrivers"] as string)
                {
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                owner.IsEnabled = false;

                blockAddDrivers.Show();

                TextBox logTextBox = CreateLogContainer();

                await Task.Run(() =>
                {
                    // 🔥 DISM OFFLINE IMAGE
                    string arguments =
                        $"/Image:\"{wcwAppState.MountWimPath}\" " +
                        $"/Add-Driver " +
                        $"/Driver:\"{_selectedFolder}\" " +
                        $"/Recurse";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    using (Process process = new Process())
                    {
                        process.StartInfo = psi;

                        process.OutputDataReceived += (s, ev) =>
                        {
                            if (string.IsNullOrWhiteSpace(ev.Data))
                                return;

                            Dispatcher.Invoke(() =>
                            {
                                AppendLog(logTextBox, ev.Data);
                            });
                        };

                        process.ErrorDataReceived += (s, ev) =>
                        {
                            if (string.IsNullOrWhiteSpace(ev.Data))
                                return;

                            Dispatcher.Invoke(() =>
                            {
                                AppendLog(logTextBox, ev.Data);
                            });
                        };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();
                        Dispatcher.Invoke(() =>
                        {
                            if (logTextBox.Text.EndsWith(Environment.NewLine))
                            {
                                logTextBox.Text =
                                    logTextBox.Text.TrimEnd('\r', '\n');
                            }

                            SaveLogs();
                        });
                    }
                });

                owner.IsEnabled = true;
                btnAddDrivers.IsEnabled = false;
                blockAddDrivers.Close();
            }
            catch (Exception ex)
            {
                this.IsEnabled = true;
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TextBox CreateLogContainer()
        {
            Border border = new Border
            {
                Padding = new Thickness(10),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = Brushes.DimGray,
                Background = _logColorToggle
                    ? new SolidColorBrush(Color.FromRgb(55, 55, 55))
                    : new SolidColorBrush(Color.FromRgb(75, 75, 75))
            };

            _logColorToggle = !_logColorToggle;

            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // 🔥 Номер + час
            _logIndex++;

            TextBlock info = new TextBlock
            {
                Text = $"{_logIndex}. {DateTime.Now:dd/MM-HH:mm:ss}",
                Foreground = (Brush)RES["GlobalTitleForeground3"]
            };

            // 🔥 HEADER (шлях папки)
            TextBlock header = new TextBlock
            {
                Text = _selectedFolder,
                Margin = new Thickness(0, 2, 0, 5),
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)RES["GlobalTitleForeground1"]
            };

            // 🔥 LOG TEXTBOX
            TextBox textBox = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = (Brush)RES["GlobalTitleForeground3"],

                IsReadOnly = true,
                IsReadOnlyCaretVisible = false,

                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,

                FontFamily = new FontFamily("Consolas")
            };

            panel.Children.Add(info);
            panel.Children.Add(header);
            panel.Children.Add(textBox);

            border.Child = panel;

            PanelADIList.Children.Add(border);

            return textBox;
        }

        private void AppendLog(TextBox textBlock, string text)
        {
            textBlock.Text += text + Environment.NewLine;
        }

        private void SaveLogs()
        {
            try
            {
                if (File.Exists(_logPath))
                {
                    File.SetAttributes(_logPath, FileAttributes.Normal);
                    File.Delete(_logPath);
                }

                StringBuilder sb = new StringBuilder();

                foreach (Border border in PanelADIList.Children)
                {
                    StackPanel panel = border.Child as StackPanel;
                    if (panel == null)
                        continue;

                    TextBlock info = panel.Children[0] as TextBlock;
                    TextBlock header = panel.Children[1] as TextBlock;
                    TextBox textBox = panel.Children[2] as TextBox;

                    sb.AppendLine("[ITEM]");
                    sb.AppendLine("INFO=" + (info != null ? info.Text : ""));
                    sb.AppendLine("HEADER=" + (header != null ? header.Text : ""));
                    sb.AppendLine("TEXT=" + (textBox != null ? textBox.Text : ""));
                    sb.AppendLine("[END]");
                    sb.AppendLine();
                }

                File.WriteAllText(_logPath, sb.ToString(), Encoding.UTF8);
                File.SetAttributes(_logPath, FileAttributes.Hidden);
            }
            catch
            {
            }
        }

        private void LoadLogsFromTxt()
        {
            try
            {
                if (!File.Exists(_logPath))
                    return;

                string[] lines = File.ReadAllLines(_logPath, Encoding.UTF8);

                string info = "";
                string header = "";

                StringBuilder textBuilder = new StringBuilder();

                bool isReadingText = false;

                foreach (string line in lines)
                {
                    if (line == "[ITEM]")
                    {
                        info = "";
                        header = "";

                        textBuilder.Clear();

                        isReadingText = false;
                    }
                    else if (line.StartsWith("INFO="))
                    {
                        info = line.Substring("INFO=".Length);
                    }
                    else if (line.StartsWith("HEADER="))
                    {
                        header = line.Substring("HEADER=".Length);
                    }
                    else if (line.StartsWith("TEXT="))
                    {
                        isReadingText = true;

                        textBuilder.AppendLine(line.Substring("TEXT=".Length));
                    }
                    else if (line == "[END]")
                    {
                        CreateRestoredLogContainer(info, header, textBuilder.ToString());
                        isReadingText = false;
                    }
                    else
                    {
                        if (isReadingText)
                        {
                            textBuilder.AppendLine(line);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void CreateRestoredLogContainer(string infoText, string headerText, string logText)
        {
            Border border = new Border
            {
                Padding = new Thickness(5),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = Brushes.Gray,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60))
            };

            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // 🔥 INFO
            TextBlock info = new TextBlock
            {
                Text = infoText,
                Foreground = Brushes.DarkGray
            };

            // 🔥 HEADER
            TextBlock header = new TextBlock
            {
                Text = headerText,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 2, 0, 5),
                FontWeight = FontWeights.SemiBold
            };

            // 🔥 TEXTBOX
            TextBox textBox = new TextBox
            {
                Text = logText,
                Foreground = Brushes.Gray,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),

                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,

                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,

                FontFamily = new FontFamily("Consolas")
            };

            // 🔥 ПОРЯДОК ДУЖЕ ВАЖЛИВИЙ
            panel.Children.Add(info);
            panel.Children.Add(header);
            panel.Children.Add(textBox);

            border.Child = panel;

            PanelADIList.Children.Add(border);
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
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