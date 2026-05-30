using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using WindowsCostumizeWizard.Windowinfo;
using WindowsCostumizeWizard.WindowInfo;

namespace WindowsCostumizeWizard
{
    public partial class App : Application
    {
        public static AppConfig Config { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);



            EnsureInternalResources();

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // ===== Видалення temp_ad?.txt =====
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                string[] tempFiles =
                {
                    Path.Combine(baseDir, "temp_ads.txt"),
                    Path.Combine(baseDir, "temp_adi.txt")
                };

                foreach (string filePath in tempFiles)
                {
                    if (File.Exists(filePath))
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                        File.Delete(filePath);
                    }
                }
            }
            catch
            {
            }

            Config = AppConfig.Load();

            // Завжди оновлюємо розмір екрану
            Config.ScreenWidth = SystemParameters.PrimaryScreenWidth;
            Config.ScreenHeight = SystemParameters.PrimaryScreenHeight;

            Config.Save();
            
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oscdimg");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            DetectOscdimg(); // просто виклик

            // ===== Перевірка/створення Wimlib =====
            string wimlibDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wimlib");

            Directory.CreateDirectory(wimlibDir);

            // ===== кеш валідний → не перевіряємо диск =====
            string wimlibPath = Path.Combine(wimlibDir, "wimlib-imagex.exe");

            if (Config.HasWimlib == true && File.Exists(wimlibPath))
            {
                wcwAppState.WimlibExists = true;
            }
            else
            {
                // ===== перевірка реального стану =====
                wcwAppState.WimlibExists = File.Exists(wimlibPath);

                // ===== оновлюємо кеш =====
                Config.HasWimlib = wcwAppState.WimlibExists;
                Config.Save();
            }

            // ===== Застосування мови та теми =====
            ApplyLanguage(Config.Language);
            ApplyTheme(Config.Theme);

            // ===== Перевірка робочої директорії =====
            bool needSelectFolder =
                string.IsNullOrEmpty(Config.WorkDirectory) || !Directory.Exists(Config.WorkDirectory);

            if (needSelectFolder)
            {
                new WindowLocationInfo((string)Application.Current.Resources["Text_StartInfoFolder"])
                    .ShowDialog();

                if (!SelectAndInitWorkDirectory(isStartup: true))
                    return;
            }

            wcwAppState.WorkDirectory = Config.WorkDirectory;

            // ===== Створення директорій WCW та MountWIM, якщо їх немає =====
            string mountPath = Path.Combine(wcwAppState.WorkDirectory, "MountWIM");
            string extractPath = Path.Combine(wcwAppState.WorkDirectory, "ExtractISO");

            Directory.CreateDirectory(mountPath);
            Directory.CreateDirectory(extractPath);

            wcwAppState.MountWimPath = mountPath;
            wcwAppState.ExtractIsoPath = extractPath;

            // ===== Перевірка чи змонтований WIM =====
            bool hasContent = Directory.EnumerateFileSystemEntries(mountPath).Any();

            wcwAppState.IsWimMounted = !hasContent;

            if (!wcwAppState.IsWimMounted)
            {
                var infoWindow = new WindowInfoFolderWim(Application.Current.Resources["Text_FolderWimWindow"] as string);

                infoWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                bool? result = infoWindow.ShowDialog();

                if (result != true)
                {
                    Shutdown();
                    return;
                }
            }

            // ===== Перевірка змонтованого ISO =====
            if (CheckMountedIso(out string isoPath))
                wcwAppState.MountedIsoPath = isoPath;
            else
                wcwAppState.MountedIsoPath = null;

            // ===== Старт MainWindow =====
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
            ShutdownMode = ShutdownMode.OnLastWindowClose;
        }

        private void EnsureInternalResources()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            ExtractResourceIfMissing(
                "WindowsCostumizeWizard.Resources.features.json",
                Path.Combine(baseDir, "features.json"));

            ExtractResourceIfMissing(
                "WindowsCostumizeWizard.Resources.MountValidation.exe",
                Path.Combine(baseDir, "MountValidation.exe"));
        }

        private void ExtractResourceIfMissing(string resourceName, string outputPath)
        {
            try
            {
                if (File.Exists(outputPath))
                    return;

                Assembly assembly = Assembly.GetExecutingAssembly();

                using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                        return;

                    using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }
            }
            catch
            {
            }
        }

        private void DetectOscdimg()
        {
            string sysPath = @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\x86\Oscdimg\oscdimg.exe";

            string progPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oscdimg", "oscdimg.exe");

            wcwAppState.OscdimgSysPath = false;
            wcwAppState.OscdimgProgPath = false;

            if (File.Exists(sysPath))
            {
                wcwAppState.OscdimgSysPath = true;

                Config.OscdimgPath = true;
                Config.OscdimgExePath = sysPath;

                Config.Save();

                return;
            }

            if (File.Exists(progPath))
            {
                wcwAppState.OscdimgProgPath = true;

                Config.OscdimgPath = true;
                Config.OscdimgExePath = progPath;

                Config.Save();

                return;
            }

            // =========================
            Config.OscdimgPath = false;
            Config.OscdimgExePath = null;

            Config.Save();
        }

        public bool CheckMountedIso(out string isoPath)
        {
            isoPath = null;
            var drive = DriveInfo.GetDrives()
                .FirstOrDefault(d => d.DriveType == DriveType.CDRom && d.IsReady);

            if (drive == null)
                return false;

            isoPath = drive.RootDirectory.FullName;
            return true;
        }

        public bool SelectAndInitWorkDirectory(bool isStartup)
        {
            string basePath = SelectWorkFolder();
            if (string.IsNullOrEmpty(basePath))
            {
                if (isStartup)
                    Shutdown();
                return false;
            }

            string wcwPath = Path.Combine(basePath, "WCW");
            Directory.CreateDirectory(wcwPath);
            Directory.CreateDirectory(Path.Combine(wcwPath, "ExtractISO"));
            Directory.CreateDirectory(Path.Combine(wcwPath, "MountWIM"));

            Config.WorkDirectory = wcwPath;
            Config.Save();
            return true;
        }

        private string SelectWorkFolder()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = Application.Current.Resources["Text_SelectWorkFolder"] as string,
                InitialDirectory = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}" // This PC
            };

            return dialog.ShowDialog() == CommonFileDialogResult.Ok ? dialog.FileName : null;
        }

        public void ApplyLanguage(string lang)
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri($"Languages/Language_{lang}.xaml", UriKind.Relative)
            };

            var merged = Resources.MergedDictionaries;
            for (int i = merged.Count - 1; i >= 0; i--)
                if (merged[i].Source?.OriginalString.Contains("Languages/") == true)
                    merged.RemoveAt(i);

            merged.Add(dict);
        }

        public void ApplyTheme(string theme)
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri($"Themes/{theme}.xaml", UriKind.Relative)
            };

            var merged = Resources.MergedDictionaries;
            for (int i = merged.Count - 1; i >= 0; i--)
                if (merged[i].Source?.OriginalString.Contains("Themes/") == true)
                    merged.RemoveAt(i);

            merged.Add(dict);
        }

        public void UpdateConfig(string lang, string theme)
        {
            Config.Language = lang;
            Config.Theme = theme;
            Config.Save();

            ApplyLanguage(lang);
            ApplyTheme(theme);
        }
    }
}