using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WindowsCostumizeWizard.Windowinfo;
using WindowsCostumizeWizard.WindowInfo;

namespace WindowsCostumizeWizard.MW_Element.UC_FPA_Element
{
    public partial class UC_Features : UserControl
    {
        private UC_All_FPA.FpaMode _mode;
        public ObservableCollection<FeatureItem> FeaturesList { get; set; }

        private int _numberEnabled;
        public int NumberEnabled
        {
            get => _numberEnabled;
            set { _numberEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberEnabled))); }
        }

        private int _numberDisabled;
        public int NumberDisabled
        {
            get => _numberDisabled;
            set { _numberDisabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberDisabled))); }
        }

        private int _numberOther;
        public int NumberOther
        {
            get => _numberOther;
            set { _numberOther = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberOther))); }
        }

        private int _numberAll;
        public int NumberAll
        {
            get => _numberAll;
            set { _numberAll = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberAll))); }
        }

        private string _nameFPA;
        private int _journalRowIndex = 0;

        public string NameFPA
        {
            get => _nameFPA;
            set { _nameFPA = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NameFPA))); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isInitialized = false;

        public UC_Features(UC_All_FPA.FpaMode mode)
        {
            InitializeComponent();
            _mode = mode;
            FeaturesList = new ObservableCollection<FeatureItem>();
            DataContext = this;
        }

        public void DisableUI(bool enabled)
        {
            this.IsEnabled = enabled;
        }

        public async Task InitAsync()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            await LoadFeatures();
        }

        private async Task LoadFeatures()
        {
            var owner = Window.GetWindow(this) ?? Application.Current.MainWindow;

            var text = Application.Current.Resources["Text_BlockLoadFeatures"] as string
                       ?? "Loading...";

            var block = new WindowProgressBlock(text)
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            owner?.Dispatcher.Invoke(() => owner.IsEnabled = false);
            block.Show();

            try
            {
                string output = await Task.Run(() =>
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = _mode == UC_All_FPA.FpaMode.Online
                            ? "/Online /Get-Features"
                            : $"/Image:\"{wcwAppState.MountWimPath}\" /Get-Features",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process == null) return string.Empty;

                        string result = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        return result;
                    }
                });

                ParseFeatures(output);
                LoadFeaturesTree();
            }
            finally
            {
                block.Close();
                owner?.Dispatcher.Invoke(() => owner.IsEnabled = true);
            }
        }

        private void ParseFeatures(string dismOutput)
        {
            FeaturesList.Clear();

            Regex matches = new Regex(@"Feature Name : (.+?)\r?\n\s*State : (.+?)\r?\n", RegexOptions.IgnoreCase);
            MatchCollection mc = matches.Matches(dismOutput);

            foreach (Match match in mc)
            {
                FeatureItem item = new FeatureItem
                {
                    Name = match.Groups[1].Value.Trim(),
                    State = match.Groups[2].Value.Trim()
                };
                FeaturesList.Add(item);
            }
        }

        private void LoadFeaturesTree()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "features.json");

            var nodes = File.Exists(path)
                ? JsonConvert.DeserializeObject<List<FeatureNode>>(File.ReadAllText(path))
                : new List<FeatureNode>();

            var dismLookup = FeaturesList.ToDictionary(x => x.Name, x => x);

            var treeLookup = new Dictionary<string, FeatureItem>();

            foreach (var node in nodes)
            {
                if (!dismLookup.TryGetValue(node.Name, out var item))
                    continue;

                treeLookup[node.Name] = item;
            }

            var rootItems = new ObservableCollection<FeatureItem>();

            foreach (var node in nodes)
            {
                if (!treeLookup.TryGetValue(node.Name, out var current))
                    continue;

                if (string.IsNullOrWhiteSpace(node.Parent))
                {
                    rootItems.Add(current);
                }
                else if (treeLookup.TryGetValue(node.Parent, out var parent))
                {
                    parent.Children.Add(current);
                }
                else
                {
                    rootItems.Add(current);
                }
            }

            foreach (var item in FeaturesList)
            {
                if (!treeLookup.ContainsKey(item.Name))
                {
                    rootItems.Add(item);
                }
            }

            SortTreeByDismOrder(rootItems, FeaturesList.Select(x => x.Name).ToList());

            FeaturesList = rootItems;

            UpdateFeatureRules();
            LockPendingFeatures();
            UpdateCounts();

            DataContext = null;
            DataContext = this;
        }

        private void LockPendingFeatures()
        {
            foreach (var item in GetAllItemsRecursive(FeaturesList))
            {
                bool isPending = item.State.Equals("Enable Pending", StringComparison.OrdinalIgnoreCase) ||
                                 item.State.Equals("Disable Pending", StringComparison.OrdinalIgnoreCase);

                if (isPending)
                {
                    item.IsEnabledCheck = false;
                }

                if (item.Children != null && item.Children.Count > 0)
                {
                    foreach (var child in GetAllItemsRecursive(item.Children))
                    {
                        bool childPending = child.State.Equals("Enable Pending", StringComparison.OrdinalIgnoreCase) ||
                                            child.State.Equals("Disable Pending", StringComparison.OrdinalIgnoreCase);

                        if (childPending)
                            child.IsEnabledCheck = false;
                    }
                }
            }
        }

        private void SortTreeByDismOrder(ObservableCollection<FeatureItem> items, List<string> dismOrder)
        {
            var sorted = new List<FeatureItem>(items);
            sorted.Sort((a, b) => dismOrder.IndexOf(a.Name).CompareTo(dismOrder.IndexOf(b.Name)));

            items.Clear();

            foreach (var item in sorted)
            {
                items.Add(item);

                if (item.Children != null && item.Children.Count > 0)
                    SortTreeByDismOrder(item.Children, dismOrder);
            }
        }

        public class FeatureNode
        {
            public string Name { get; set; }
            public string Parent { get; set; }
        }

        private void OpenNetFx3Source_Click(object sender, RoutedEventArgs e)
        {
            var settings = new W_NetFx3CabPickerWindow();
            settings.ShowDialog();
        }

        private void CheckBox_StateChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is FeatureItem item)
            {
                UpdateOppositeRules();
                UpdateParentChildRules();
            }
        }

        private void UpdateOppositeRules()
        {
            var allItems = GetAllItemsRecursive(FeaturesList).ToList();

            bool anyEnabledChecked = allItems
                .Any(x => x.IsChecked && x.State.Equals("Enabled", StringComparison.OrdinalIgnoreCase));

            bool anyDisabledGroupChecked = allItems
                .Any(x => x.IsChecked &&
                    (x.State.Equals("Disabled", StringComparison.OrdinalIgnoreCase) ||
                     x.State.Equals("Disabled with Payload Removed", StringComparison.OrdinalIgnoreCase)));

            foreach (var item in allItems)
            {
                if (item.State.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsEnabledCheck = !anyDisabledGroupChecked;
                }
                else if (item.State.Equals("Disabled", StringComparison.OrdinalIgnoreCase) ||
                         item.State.Equals("Disabled with Payload Removed", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsEnabledCheck = !anyEnabledChecked;
                }
                else
                {
                    item.IsEnabledCheck = true;
                }
            }

            OffFeatures.IsEnabled = anyEnabledChecked;
            OnFeatures.IsEnabled = anyDisabledGroupChecked;
        }

        public async Task RefreshFeatures()
        {
            FeaturesList.Clear();
            await LoadFeatures();
            LoadFeaturesTree();
        }

        private async void UpdateFeature_Click(object sener, RoutedEventArgs e)
        {
            await RefreshFeatures();
        }

        public void UpdateJournalColors()
        {
            foreach (var child in PanelFeaturesList.Children)
            {
                if (child is Grid grid)
                {
                    if (grid.Tag != null)
                    {
                        switch (grid.Tag.ToString())
                        {
                            case "jurnal0":
                                grid.Background = (Brush)FindResource("JurnalBackground0");
                                break;

                            case "jurnal1":
                                grid.Background = (Brush)FindResource("JurnalBackground1");
                                break;
                        }
                    }

                    foreach (var element in grid.Children)
                    {
                        if (element is TextBlock tb && tb.Tag != null)
                        {
                            switch (tb.Tag.ToString())
                            {
                                case "Name":
                                    tb.Foreground = (Brush)FindResource("GlobalTitleForeground3");
                                    break;

                                case "Separator":
                                    tb.Foreground = (Brush)FindResource("Separator");
                                    break;

                                case "Progress":
                                    tb.Foreground = (Brush)FindResource("ColorProgres");
                                    break;

                                case "infoBox":
                                    tb.Foreground = (Brush)FindResource("GlobalTitleForeground2");
                                    break;
                            }
                        }

                        if (element is TextBox tx && tx.Tag != null)
                        {
                            if (tx.Tag.ToString() == "infoBox")
                            {
                                tx.Foreground = (Brush)FindResource("GlobalTitleForeground2");
                            }
                        }
                    }
                }
            }
        }

        private async void EnableFeatures_Click(object sender, RoutedEventArgs e)
        {
            if (cbConsoleView.IsChecked == true)
                await ProcessFeatures1(true);
            else
                await ProcessFeatures0(true);
        }

        private async void DisableFeatures_Click(object sender, RoutedEventArgs e)
        {
            if (cbConsoleView.IsChecked == true)
                await ProcessFeatures1(false);
            else
                await ProcessFeatures0(false);
        }

        private async Task ProcessFeatures0(bool enable)
        {
            var features = GetAllItemsRecursive(FeaturesList)
                .Where(x => x.IsChecked &&
                       (enable
                            ? x.State.Equals("Disabled", StringComparison.OrdinalIgnoreCase)
                            : x.State.Equals("Enabled", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (!features.Any()) return;

            Window.GetWindow(this).IsEnabled = false;

            try
            {
                foreach (var feature in features)
                {
                    TextBlock progressBlock = null;
                    TextBox infoBox = null;
                    TextBlock impl = null;

                    Dispatcher.Invoke(() =>
                    {
                        bool even = (_journalRowIndex % 2 == 0);

                        Grid grid = new Grid
                        {
                            Background = (Brush)FindResource(even ? "JurnalBackground0" : "JurnalBackground1"),
                            Tag = even ? "jurnal0" : "jurnal1"
                        };

                        _journalRowIndex++;

                        for (int i = 0; i < 7; i++)
                            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        var name = new TextBlock
                        {
                            Text = feature.Name,
                            Foreground = (Brush)FindResource("GlobalTitleForeground3"),
                            Margin = new Thickness(5, 5, 5, 0),
                            Tag = "Name"
                        };
                        Grid.SetRow(name, 0);

                        var sep0 = new TextBlock
                        {
                            Text = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)FindResource("Separator"),
                            Margin = new Thickness(5, 0, 5, 0),
                            Tag = "Separator"
                        };
                        Grid.SetRow(sep0, 1);

                        var sep1 = new TextBlock
                        {
                            Text = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)FindResource("Separator"),
                            Margin = new Thickness(5, 0, 5, 0),
                            Tag = "Separator"
                        };
                        Grid.SetRow(sep1, 3);

                        progressBlock = new TextBlock
                        {
                            Foreground = (Brush)FindResource("ColorProgres"),
                            Margin = new Thickness(5, 0, 5, 0),
                            Tag = "Progress"
                        };
                        Grid.SetRow(progressBlock, 4);

                        var sep2 = new TextBlock
                        {
                            Text = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)FindResource("Separator"),
                            Margin = new Thickness(5, 0, 5, 0),
                            Tag = "Separator"
                        };
                        Grid.SetRow(sep2, 5);

                        infoBox = new TextBox
                        {
                            Background = Brushes.Transparent,
                            BorderBrush = Brushes.Transparent,
                            IsReadOnly = true,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = (Brush)FindResource("GlobalTitleForeground2"),
                            Margin = new Thickness(5, 0, 5, 0),
                            Tag = "infoBox"
                        };
                        Grid.SetRow(infoBox, 6);

                        grid.Children.Add(name);
                        grid.Children.Add(sep0);
                        grid.Children.Add(sep1);
                        grid.Children.Add(progressBlock);
                        grid.Children.Add(sep2);
                        grid.Children.Add(infoBox);

                        PanelFeaturesList.Children.Add(grid);

                        var parentScrollViewer = PanelFeaturesList.Parent as ScrollViewer;
                        parentScrollViewer?.ScrollToEnd();
                    });

                    string imageArgument = _mode == UC_All_FPA.FpaMode.Online
                        ? "/Online"
                        : $"/Image:\"{wcwAppState.MountWimPath}\"";

                    string action = enable ? "Enable-Feature" : "Disable-Feature";

                    string arguments = $"{imageArgument} /{action} /FeatureName:\"{feature.Name}\" /NoRestart";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = new Process { StartInfo = psi, EnableRaisingEvents = true })
                    {
                        process.OutputDataReceived += (s, e2) =>
                        {
                            if (string.IsNullOrWhiteSpace(e2.Data)) return;

                            string line = e2.Data.Trim();

                            if (line.StartsWith("Deployment Image Servicing and Management tool") ||
                                line.StartsWith("Version:") ||
                                line.StartsWith("Image Version:"))
                                return;

                            bool isProgress = line.Contains("%") && line.Contains("[") && line.Contains("]");

                            Dispatcher.Invoke(() =>
                            {
                                if (impl == null)
                                {
                                    impl = new TextBlock
                                    {
                                        Text = line,
                                        Foreground = (Brush)FindResource("GlobalTitleForeground2"),
                                        Margin = new Thickness(5, 0, 5, 0),
                                        Tag = "infoBox"
                                    };
                                    Grid.SetRow(impl, 2);
                                    ((Grid)progressBlock.Parent).Children.Add(impl);
                                }
                                else if (isProgress)
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

                await RefreshFeatures();
                TextStatusFeaturesPC.Text = Application.Current.Resources["Text_RestartPC"] as string;
            }
            finally
            {
                Window.GetWindow(this).IsEnabled = true;
                OnFeatures.IsEnabled = false;
            }
        }

        private async Task ProcessFeatures1(bool enable)
        {
            var features = GetAllItemsRecursive(FeaturesList)
                .Where(x => x.IsChecked &&
                       (enable
                            ? x.State.Equals("Disabled", StringComparison.OrdinalIgnoreCase)
                            : x.State.Equals("Enabled", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (!features.Any()) return;

            // 🔹 Оголошуємо action тут
            string action = enable ? "Enable-Feature" : "Disable-Feature";

            // 🔹 Формуємо аргумент для Online / Offline
            string imageArgument = _mode == UC_All_FPA.FpaMode.Online
                ? "/Online"
                : $"/Image:\"{wcwAppState.MountWimPath}\"";

            string allCommands = string.Join(" & timeout /t 2 /nobreak >nul & ",
                features.Select(f => $"dism {imageArgument} /{action} /FeatureName:\"{f.Name}\""));

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k {allCommands}",
                UseShellExecute = true,
                Verb = "runas"
            };

            Window.GetWindow(this).IsEnabled = false;

            var process = Process.Start(psi);
            if (process == null) return;

            process.EnableRaisingEvents = true;

            process.Exited += async (s, ev) =>
            {
                await Dispatcher.Invoke(async () =>
                {
                    Window.GetWindow(this).IsEnabled = true;
                    await RefreshFeatures();

                    if (enable)
                        OnFeatures.IsEnabled = false;
                    else
                        OffFeatures.IsEnabled = false;
                });
            };
        }

        private void UpdateParentChildRules()
        {
            foreach (var root in FeaturesList)
                ApplyParentChildRules(root);
        }

        private void ApplyParentChildRules(FeatureItem parent)
        {
            bool parentDisabled = parent.State.Equals("Disabled", StringComparison.OrdinalIgnoreCase);

            foreach (var child in parent.Children)
            {
                child.IsEnabledCheck = !parentDisabled && child.IsEnabledCheck;

                ApplyParentChildRules(child);
            }
        }

        private void UpdateFeatureRules()
        {
            foreach (var root in FeaturesList)
                ApplyParentStateRules(root);
        }

        private void ApplyParentStateRules(FeatureItem parent)
        {
            bool parentDisabled = parent.State.Equals("Disabled", StringComparison.OrdinalIgnoreCase);

            foreach (var child in parent.Children)
            {
                child.IsEnabledCheck = !parentDisabled;

                ApplyParentStateRules(child);
            }
        }

        private void UpdateCounts()
        {
            var allItems = GetAllItemsRecursive(FeaturesList).ToList();

            NumberEnabled = allItems.Count(x => x.State?.Equals("Enabled", StringComparison.OrdinalIgnoreCase) == true);
            NumberDisabled = allItems.Count(x => x.State?.Equals("Disabled", StringComparison.OrdinalIgnoreCase) == true);
            NumberOther = allItems.Count - NumberEnabled - NumberDisabled;
            NumberAll = allItems.Count;

            NameFPA = Application.Current.Resources["Text_NameFpaFeatures"] as string;
        }

        private IEnumerable<FeatureItem> GetAllItemsRecursive(IEnumerable<FeatureItem> items)
        {
            foreach (var item in items)
            {
                yield return item;

                if (item.Children != null && item.Children.Count > 0)
                {
                    foreach (var child in GetAllItemsRecursive(item.Children))
                        yield return child;
                }
            }
        }
    }
}