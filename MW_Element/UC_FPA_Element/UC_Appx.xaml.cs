using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WindowsCostumizeWizard.Windowinfo;
using WindowsCostumizeWizard.WindowInfo;

namespace WindowsCostumizeWizard.MW_Element.UC_FPA_Element
{
    public partial class UC_Appx : UserControl
    {
        private UC_All_FPA.FpaMode _mode;
        public ObservableCollection<AppxItem> Packages { get; private set; }
        private int lastCheckedIndex = -1;
        private int _journalRowIndexAppx = 0;
        private bool _isInitialized = false;

        public UC_Appx(UC_All_FPA.FpaMode mode)
        {
            InitializeComponent();
            _mode = mode;
            Packages = new ObservableCollection<AppxItem>();
            AppxListView.ItemsSource = Packages;
        }

        private void SearchBoxAppx_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            bool isValid = Regex.IsMatch(e.Text, @"^[a-zA-Z0-9_\-\.]+$");

            if (!isValid)
            {
                SystemSounds.Beep.Play();
                e.Handled = true;
            }
        }

        private void SearchBoxAppx_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBoxAppx.Text.ToLower();

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(AppxListView.ItemsSource);

            view.Filter = item =>
            {
                if (item is AppxItem package)
                {
                    if (string.IsNullOrWhiteSpace(searchText))
                        return true;

                    return package.PackageName != null &&
                           package.PackageName.ToLower().Contains(searchText);
                }

                return false;
            };
        }

        public async Task InitAsync()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            await LoadAppxAsync(); // або Packages/Appx
        }

        private void UpdateRemoveButtonState()
        {
            btnRemoveAppx.IsEnabled = Packages.Any(x => x.IsChecked);
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb == null) return;

            int currentIndex = AppxListView.Items.IndexOf(cb.DataContext);
            if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && lastCheckedIndex >= 0)
            {
                int start = Math.Min(lastCheckedIndex, currentIndex);
                int end = Math.Max(lastCheckedIndex, currentIndex);

                for (int i = start; i <= end; i++)
                {
                    if (AppxListView.Items[i] is AppxItem item)
                    {
                        item.IsChecked = true;
                        item.IsSelected = true;
                    }
                }
            }
            else
            {
                if (cb.DataContext is AppxItem item)
                {
                    item.IsChecked = cb.IsChecked ?? false;
                    item.IsSelected = cb.IsChecked ?? false;
                }
            }

            lastCheckedIndex = currentIndex;
            UpdateRemoveButtonState();
        }

        private void SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            if (AppxListView.Items == null) return;

            bool check = SelectAll.IsChecked ?? false;

            foreach (var item in AppxListView.Items)
            {
                if (item is AppxItem package)
                {
                    package.IsChecked = check;
                    package.IsSelected = check;
                }
            }
            UpdateRemoveButtonState();
        }

        private void AppxListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (AppxItem item in Packages)
            {
                if (!AppxListView.SelectedItems.Contains(item))
                {
                    if (item.IsChecked == false)
                        item.IsSelected = false;
                }
            }
        }

        private async void UpdatePackages_Click(object sender, RoutedEventArgs e)
        {
            Packages.Clear();
            await LoadAppxAsync();
        }

        private async Task LoadAppxAsync()
        {
            var owner = Window.GetWindow(this);

            var block = new WindowProgressBlock(
                Application.Current.Resources["Text_BlockLoadFeatures"] as string)
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            owner.IsEnabled = false; // 🔴 БЛОКУЄМО ВСЕ ВІКНО
            block.Show();

            try
            {
                string[] lines = await Task.Run(() =>
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "dism.exe";

                    if (_mode == UC_All_FPA.FpaMode.Online)
                    {
                        psi.Arguments = "/Online /Get-ProvisionedAppxPackages";
                    }
                    else
                    {
                        psi.Arguments = $"/Image:\"{wcwAppState.MountWimPath}\" /Get-ProvisionedAppxPackages";
                    }

                    psi.RedirectStandardOutput = true;
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.StandardOutputEncoding = System.Text.Encoding.UTF8;

                    using (Process process = Process.Start(psi))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        return output.Split(
                            new[] { Environment.NewLine },
                            StringSplitOptions.RemoveEmptyEntries
                        );
                    }
                });

                AppxItem current = null;

                foreach (var line in lines)
                {
                    string trimmed = line.Trim();

                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    if (trimmed.StartsWith("PackageName", StringComparison.OrdinalIgnoreCase))
                    {
                        int maxNumber = Packages.Count;
                        int maxDigits = Math.Max(2, (int)Math.Ceiling(Math.Log10(maxNumber + 1)));

                        current = new AppxItem
                        {
                            PackageName = GetValue(trimmed),
                            Number = (Packages.Count + 1).ToString($"D{maxDigits}")
                        };

                        Packages.Add(current);
                    }
                    else if (current != null)
                    {
                        if (trimmed.StartsWith("Architecture", StringComparison.OrdinalIgnoreCase))
                            current.Architecture = GetValue(trimmed);
                        else if (trimmed.StartsWith("Version", StringComparison.OrdinalIgnoreCase))
                            current.Version = GetValue(trimmed);
                    }
                }

                AllAppx.Text = $"{Packages.Count}";
            }
            catch (Exception)
            {
                new WindowMessageInfoError(Application.Current.Resources["Text_LoadErrorOnlineAppx"] as string)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                }.Show();
            }
            finally
            {
                block.Close();
                owner.IsEnabled = true; // 🔴 РОЗБЛОКУВАЛИ
            }
        }

        private string GetValue(string line)
        {
            int idx = line.IndexOf(':');
            if (idx >= 0 && idx + 1 < line.Length)
                return line.Substring(idx + 1).Trim();
            return string.Empty;
        }

        private async void RemoveAppx_Click(object sender, RoutedEventArgs e)
        {
            if (cbConsoleView.IsChecked == true)
                await ProcessAppx1();
            else
                await ProcessAppx0();
        }

        private async Task ProcessAppx0()
        {
            btnRemoveAppx.IsEnabled = false;
            var packagesToRemove = Packages
                .Where(x => x.IsChecked)
                .ToList();

            if (!packagesToRemove.Any()) return;

            Window.GetWindow(this).IsEnabled = false;

            try
            {
                foreach (var package in packagesToRemove)
                {
                    TextBlock progressBlock = null;
                    TextBox infoBox = null;
                    TextBlock impl = null;

                    Dispatcher.Invoke(() =>
                    {
                        bool even = (_journalRowIndexAppx % 2 == 0);

                        Grid grid = new Grid
                        {
                            Background = (Brush)FindResource(even ? "JurnalBackground0" : "JurnalBackground1"),
                            Tag = even ? "jurnal0" : "jurnal1"
                        };

                        _journalRowIndexAppx++;

                        for (int i = 0; i < 5; i++)
                            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        impl = new TextBlock
                        {
                            Foreground = (Brush)FindResource("GlobalTitleForeground3"),
                            Margin = new Thickness(5, 0, 5, 0),
                            Tag = "Name"
                        };
                        Grid.SetRow(impl, 0);

                        var sep0 = new TextBlock
                        {
                            Text = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)FindResource("Separator"),
                            Margin = new Thickness(5, 0, 5, 0),
                            Tag = "Separator"
                        };
                        Grid.SetRow(sep0, 1);

                        progressBlock = new TextBlock
                        {
                            Foreground = (Brush)FindResource("ColorProgres"),
                            Margin = new Thickness(5, 0, 5, 0),
                            Tag = "Progress"
                        };
                        Grid.SetRow(progressBlock, 2);

                        var sep1 = new TextBlock
                        {
                            Text = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)FindResource("Separator"),
                            Margin = new Thickness(5, 0, 5, 0),
                            Tag = "Separator"
                        };
                        Grid.SetRow(sep1, 3);

                        infoBox = new TextBox
                        {
                            Background = Brushes.Transparent,
                            BorderBrush = Brushes.Transparent,
                            IsReadOnly = true,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = (Brush)FindResource("GlobalTitleForeground2"),
                            Margin = new Thickness(5, 0, 5, 0),
                            Tag = "InfoBox"
                        };
                        Grid.SetRow(infoBox, 4);

                        grid.Children.Add(impl);
                        grid.Children.Add(sep0);
                        grid.Children.Add(progressBlock);
                        grid.Children.Add(sep1);
                        grid.Children.Add(infoBox);

                        PanelPackagesList.Children.Add(grid);

                        (PanelPackagesList.Parent as ScrollViewer)?.ScrollToEnd();
                    });

                    string dismArgs = _mode == UC_All_FPA.FpaMode.Online
                        ? $"/Online /Remove-ProvisionedAppxPackage /PackageName:\"{package.PackageName}\" /NoRestart"
                        : $"/Image:\"{wcwAppState.MountWimPath}\" /Remove-ProvisionedAppxPackage /PackageName:\"{package.PackageName}\" /NoRestart";

                    // Використовуємо cmd.exe, але з редіректом stdout для стрімінгу
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c dism {dismArgs}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        Verb = "runas"
                    };

                    using (var process = new Process { StartInfo = psi, EnableRaisingEvents = true })
                    {
                        process.OutputDataReceived += (s, e2) =>
                        {
                            if (string.IsNullOrWhiteSpace(e2.Data)) return;

                            string line = e2.Data.Trim();

                            // Фільтр шапки та версій DISM
                            if (line.StartsWith("Deployment Image Servicing and Management tool", StringComparison.OrdinalIgnoreCase) ||
                                line.StartsWith("Version:", StringComparison.OrdinalIgnoreCase) ||
                                line.StartsWith("Image Version:", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }

                            Dispatcher.Invoke(() =>
                            {
                                if (string.IsNullOrEmpty(impl.Text))
                                {
                                    impl.Text = line;
                                }
                                else if (line.Contains("%") && line.Contains("[") && line.Contains("]"))
                                {
                                    progressBlock.Text = line;
                                }
                                else
                                {
                                    infoBox.AppendText(line + Environment.NewLine);
                                }
                            });
                        };

                        process.ErrorDataReceived += (s, e2) =>
                        {
                            if (!string.IsNullOrWhiteSpace(e2.Data))
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    infoBox.AppendText("[ERR] " + e2.Data + Environment.NewLine);
                                });
                            }
                        };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        await Task.Run(() => process.WaitForExit());
                    }
                }

                Packages.Clear();
                await LoadAppxAsync();
            }
            finally
            {
                Window.GetWindow(this).IsEnabled = true;
            }
        }

        private async Task ProcessAppx1()
        {
            btnRemoveAppx.IsEnabled = false;
            var packagesToRemove = Packages
                .Where(x => x.IsChecked)
                .ToList();

            if (!packagesToRemove.Any()) return;

            Window.GetWindow(this).IsEnabled = false;

            try
            {
                foreach (var p in packagesToRemove) // <-- додаємо цикл
                {
                    string allCommands = _mode == UC_All_FPA.FpaMode.Online
                        ? $"/Online /Remove-Package /PackageName:\"{p.PackageName}\" /NoRestart"
                        : $"/Image:\"{wcwAppState.MountWimPath}\" /Remove-Package /PackageName:\"{p.PackageName}\" /NoRestart";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/k {allCommands}",
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    var process = Process.Start(psi);
                    if (process == null) continue;

                    process.EnableRaisingEvents = true;

                    process.Exited += async (s, ev) =>
                    {
                        await Dispatcher.InvokeAsync(async () =>
                        {
                            Packages.Clear();
                            await LoadAppxAsync();
                        });
                    };
                }
            }
            finally
            {
                Window.GetWindow(this).IsEnabled = true;
            }
        }
    }
}