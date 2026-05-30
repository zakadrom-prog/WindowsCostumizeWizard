using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Threading;
using WindowsCostumizeWizard.Windowinfo;
using WindowsCostumizeWizard.WindowInfo;

namespace WindowsCostumizeWizard.MW_Element
{
    public partial class UC_ISO : UserControl
    {
        public UC_ISO()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            // ExplorerA — ISO
            if (!string.IsNullOrEmpty(wcwAppState.MountedIsoPath)) ExplorerA.CurrentPath = wcwAppState.MountedIsoPath;

            LoadExtractIsoIfExists();
        }

        public void LoadExtractIsoIfExists()
        {
            string extractPath = Path.Combine(App.Config.WorkDirectory, "ExtractISO");

            if (Directory.Exists(extractPath) && Directory.EnumerateFileSystemEntries(extractPath).Any())
            {
                ExplorerB.CurrentPath = extractPath;
            }
        }

        private void SetExplorerEnabled(Control explorer, bool enabled)
        {
            explorer.IsEnabled = enabled;
            explorer.Opacity = enabled ? 1.0 : 0.1; // підбираєш свою непрозорість
        }

        public void SetIsoUiStateA(bool enabled)
        {
            btnUnmountISO.IsEnabled = enabled;
            btnCopyISO.IsEnabled = enabled;
            btnOpenMountDisk.IsEnabled = enabled;
            ExplorerA.IsEnabled = enabled;
            ExplorerA.Opacity = enabled ? 1.0 : 0.1;
        }

        public void SetIsoUiStateB(bool enabled)
        {
            btnOpenIsoCopy.IsEnabled = enabled;
            btnDeleteIsoCopy.IsEnabled = enabled;
            ExplorerB.IsEnabled = enabled;
            ExplorerB.Opacity = enabled ? 1.0 : 0.1;
        }

        public void RefreshExplorerB()
        {
            ExplorerB.Refresh();
        }

        private async void Click_OpenISOFile(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this);

            var owner = Window.GetWindow(this);

            // 1. Демонтаж попереднього диска (якщо є)
            if (!string.IsNullOrEmpty(wcwAppState.MountedIsoPath))
            {
                SetIsoUiStateA(false);

                var blockUnmount = new WindowProgressBlock(Application.Current.Resources["Text_BlockUnmount"] as string)
                {
                    Owner = Window.GetWindow(this),
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                owner.IsEnabled = false;
                blockUnmount.Show();

                try
                {
                    await Task.Run(() =>
                    {
                        IsoMounter.Unmount(); // важка операція в фоні
                    });

                    // Після завершення фонової роботи оновлюємо UI
                    Dispatcher.Invoke(() =>
                    {
                        ExplorerA.CurrentPath = null;
                        SetExplorerEnabled(ExplorerA, false);
                    });
                }
                finally
                {
                    blockUnmount.Close();
                    owner.IsEnabled = true;
                }
            }

            // 2. Вибір ISO
            var dialog = new OpenFileDialog { Filter = "ISO (*.iso)|*.iso" };
            if (dialog.ShowDialog() != true) return;

            // 3. Монтування нового ISO
            var blockMount = new WindowProgressBlock(Application.Current.Resources["Text_BlockMount"] as string)
            {
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            owner.IsEnabled = false;
            blockMount.Show();

            try
            {
                bool mountSuccess = false;

                await Task.Run(() =>
                {
                    mountSuccess = IsoMounter.Mount(dialog.FileName);
                });

                Dispatcher.Invoke(() =>
                {
                    if (mountSuccess)
                    {
                        ExplorerA.CurrentPath = wcwAppState.MountedIsoPath;
                        SetIsoUiStateA(true);
                    }
                    else
                    {
                        var infoWindow = new WindowMessageInfoError(Application.Current.Resources["Text_ErrorMountISO"] as string);
                        
                        infoWindow.Owner = owner; // ВАЖЛИВО
                        infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        infoWindow.ShowDialog();
                        SetIsoUiStateA(false);
                    }
                    // десь тут потрібен напевно(я не впевнений) якийсь return чи що воно там каже про ппродовження роботи програми?
                });
            }
            finally
            {
                owner.IsEnabled = true;
                blockMount.Close();
            }
        }

        private async void Click_UnmountISO(object sender, RoutedEventArgs e)
        {
            SetIsoUiStateA(false);

            var owner = Window.GetWindow(this);
            var block = new WindowProgressBlock(Application.Current.Resources["Text_BlockUnmount"] as string)
            {
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            owner.IsEnabled = false;
            block.Show();

            try
            {
                await Task.Run(() =>
                {
                    IsoMounter.Unmount();
                });

                Dispatcher.Invoke(() =>
                {
                    ExplorerA.CurrentPath = null;
                    SetExplorerEnabled(ExplorerA, false);
                });
            }
            finally
            {
                owner.IsEnabled = true;
                block.Close();
            }

            UpdateOpenIsoButtonState();
        }

        public void UpdateOpenIsoButtonState()
        {
            // Шлях до ExtractISO
            string extractPath = Path.Combine(wcwAppState.ExtractIsoPath);

            // Чи є дані в папці
            bool hasExtractIso =
                Directory.Exists(extractPath) &&
                Directory.EnumerateFileSystemEntries(extractPath).Any();

            // Логіка кнопки: якщо папка порожня → можна відкривати ISO
            btnOpenIso.IsEnabled = !hasExtractIso;
        }

        private void Click_OpenMountDisk(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(wcwAppState.MountedIsoPath))
                return;

            System.Diagnostics.Process.Start("explorer.exe", wcwAppState.MountedIsoPath);
        }

        private void Click_CopyImageISO(object sender, RoutedEventArgs e)
        {
            btnCopyISO.IsEnabled = false;
            string source = wcwAppState.MountedIsoPath;
            string target = Path.Combine(App.Config.WorkDirectory, "ExtractISO");

            try
            {
                bool success = CD_IsoHelper.CopyIsoSystemShell(source, target);
                if (!success)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var infoWindow = new WindowMessageInfoError(Application.Current.Resources["Text_ErrorCopyISO"] as string);

                        infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        infoWindow.Show();
                    }));

                    return;
                }

                btnOpenIso.IsEnabled = false;
                SetExplorerEnabled(ExplorerB, true);
                btnOpenIsoCopy.IsEnabled = true;
                btnDeleteIsoCopy.IsEnabled = true;
                LoadExtractIsoIfExists();

                if (Application.Current.MainWindow is MainWindow mw)
                {
                    mw.CheckStatusIsoState();
                }
            }
            catch
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var infoWindow = new WindowMessageInfoError(Application.Current.Resources["Text_ErrorCopy"] as string);

                    infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    infoWindow.Show();
                }));

                btnCopyISO.IsEnabled = true;

                if (Application.Current.MainWindow is MainWindow mw)
                {
                    mw.UpdateCopyIsoButtonState();
                    mw.CheckStatusIsoState();
                }
            }
        }

        private void Click_DeleteImageISO(object sender, RoutedEventArgs e)
        {
            string extractDir = Path.Combine(App.Config.WorkDirectory, "ExtractISO");

            bool deleted = CD_IsoHelper.DeleteExtractIsoShell(extractDir);
            if (!deleted)
                return; 

            ExplorerB.CurrentPath = null;
            ExplorerB.ClearItems();
            SetIsoUiStateB(false);
            btnCopyISO.IsEnabled = true;
            btnOpenIso.IsEnabled = true;
            LoadExtractIsoIfExists();

            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.UpdateCopyIsoButtonState();
                mw.WD_UpdateButtonState();
                mw.CheckStatusIsoState();
            }
        }

        private void Click_OpenCopyIso(object sender, RoutedEventArgs e)
        {
            string extractPath = Path.Combine(App.Config.WorkDirectory, "ExtractISO");

            if (!Directory.Exists(extractPath))
                return;

            System.Diagnostics.Process.Start("explorer.exe", extractPath);
        }
    }
}
