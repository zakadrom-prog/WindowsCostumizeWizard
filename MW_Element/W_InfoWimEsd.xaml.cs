using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WindowsCostumizeWizard.MW_Element
{
    public partial class W_InfoWimEsd : Window
    {
        private bool isGray = true;
        
        public W_InfoWimEsd()
        {
            InitializeComponent();
            LoadInstallIndex();
        }

        private void LoadSelectIndexInfo_Click(object sender, RoutedEventArgs e)
        {
            var selected = ComboIndex.SelectedItem as WimIndexInfo;
            if (selected == null) return;

            string isoPath = wcwAppState.ExtractIsoPath;
            string installPath = GetInstallPath(isoPath);

            string output = RunCmd($"dism /English /Get-WimInfo /WimFile:\"{installPath}\" /index:{selected.Index}");

            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            var allowed = new HashSet<string>
            {
                "Index", "Name", "Description", "Size",
                "WIM Bootable", "Architecture", "Hal", "Version",
                "ServicePack Build", "ServicePack Level",
                "Edition", "Installation", "ProductType",
                "ProductSuite", "System Root",
                "Directories", "Files",
                "Created", "Modified", "Languages"
            };

            StringBuilder filtered = new StringBuilder();

            bool isLanguageBlock = false;

            foreach (var line in lines)
            {
                string l = line.Trim();

                if (l.StartsWith("Version:"))
                    continue;

                if (l.StartsWith("Languages"))
                {
                    filtered.AppendLine(l);
                    isLanguageBlock = true;
                    continue;
                }

                if (isLanguageBlock)
                {
                    if (string.IsNullOrWhiteSpace(l))
                    {
                        isLanguageBlock = false;
                        continue;
                    }

                    filtered.AppendLine("   " + l);
                    continue;
                }

                if (!l.Contains(":"))
                    continue;

                string key = l.Split(':')[0].Trim();

                if (allowed.Contains(key))
                    filtered.AppendLine(l);
            }

            AddBorder(filtered.ToString());
        }

        private void AddBorder(string text)
        {
            Brush consoleBrush = (Brush)Application.Current.Resources["GlobalConsoleColor"];

            Border border = new Border
            {
                Background = isGray ? Brushes.Transparent : consoleBrush,
                Padding = new Thickness(5),
            };

            TextBox tb = new TextBox
            {
                Text = text.TrimEnd('\r', '\n'),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Background = Brushes.Transparent,
                FontFamily = new FontFamily("Tahoma"),
                Foreground = (Brush)Application.Current.Resources["GlobalTitleForeground1"],
                BorderThickness = new Thickness(0)
            };

            border.Child = tb;

            ViewerB.Children.Add(border);

            isGray = !isGray;
        }

        private void CleanList_Click(object sender, RoutedEventArgs e)
        {
            // Очищаємо всі бордери з StackPanel
            ViewerB.Children.Clear();
        }

        private void LoadInstallIndex()
        {
            string isoPath = wcwAppState.ExtractIsoPath;
            string installPath = GetInstallPath(isoPath);

            if (installPath == null)
                return;

            TextStatusWim.Text = Path.GetFileName(installPath);

            string output = RunCmd($"dism /English /Get-WimInfo /WimFile:\"{installPath}\"");

            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            List<WimIndexInfo> list = new List<WimIndexInfo>();

            int currentIndex = 0;
            string currentName = "";

            StringBuilder viewerA = new StringBuilder();

            bool first = true;

            foreach (var line in lines)
            {
                string l = line.Trim();

                if (l.StartsWith("Version:"))
                    continue;

                if (l.StartsWith("Index :"))
                {
                    if (!first)
                        viewerA.AppendLine("");

                    first = false;

                    currentIndex = int.Parse(l.Split(':')[1].Trim());
                    viewerA.AppendLine(l);
                }
                else if (l.StartsWith("Name :"))
                {
                    currentName = l.Split(':')[1].Trim();
                    viewerA.AppendLine(l);

                    list.Add(new WimIndexInfo
                    {
                        Index = currentIndex,
                        Name = currentName
                    });
                }
                else if (l.StartsWith("Description :") || l.StartsWith("Size :"))
                {
                    viewerA.AppendLine(l);
                }
            }

            ViewerA.Text = viewerA.ToString();

            ComboIndex.ItemsSource = list;
            ComboIndex.SelectedIndex = 0;
        }

        private string GetInstallPath(string isoPath)
        {
            string wim = Path.Combine(isoPath, "sources", "install.wim");
            string esd = Path.Combine(isoPath, "sources", "install.esd");

            if (File.Exists(wim)) return wim;
            if (File.Exists(esd)) return esd;

            return null;
        }

        private string RunCmd(string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.GetEncoding(866) // 👈 ВАЖЛИВО!
            };

            using (var process = Process.Start(psi))
            {
                return process.StandardOutput.ReadToEnd();
            }
        }

        public class WimIndexInfo
        {
            public int Index { get; set; }
            public string Name { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        private void WindowClose_Click(object sender, RoutedEventArgs e)
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
