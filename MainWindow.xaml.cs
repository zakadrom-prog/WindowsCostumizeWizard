using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WindowsCostumizeWizard.MW_Element;
using WindowsCostumizeWizard.MW_Element.UC_FPA_Element;
using WindowsCostumizeWizard.WindowInfo;

namespace WindowsCostumizeWizard
{
    public partial class MainWindow : Window
    {
        private bool _isClosing = false;

        public MainWindow()
        {
            InitializeComponent();
            var config = App.Config;
            MaxHeight = config.ScreenHeight;
            CheckAdminRights();
            uc_iso.SetIsoUiStateA(!string.IsNullOrEmpty(wcwAppState.MountedIsoPath));
            CheckWimlibOscdimgState();
            CheckStatusIsoState();
            this.Activated += Window_Activated;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CheckUpdates();
        }

        public void CheckUpdates()
        {
            AppConfig config = AppConfig.Load();

            if (!config.WindowUpdate)
                return;

            new UpdateChecker().CheckUpdates();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (_isClosing)
                return;

            var duration = TimeSpan.FromMilliseconds(300);

            var animX = new DoubleAnimation
            {
                From = 1,
                To = 0.01,
                Duration = duration
            };

            var animY = new DoubleAnimation
            {
                From = 1,
                To = 0.01,
                Duration = duration
            };

            animY.Completed += (s, _) =>
            {
                _isClosing = true;
                Close();
            };

            WindowScale.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
            WindowScale.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void CheckAdminRights()
        {
            var config = AppConfig.Load();
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            string username = identity.Name.Split('\\').Last();
            if (string.Equals(username, "Administrator", StringComparison.OrdinalIgnoreCase))
                username = "Administrator";
            else
                username = "User";

            if (isAdmin)
            {
                if (!config.WasRunAsAdmin)
                {
                    string text = Application.Current.Resources["Text_Administration"] as string;
                    new WindowAdministrator(text, "/Resources/Images/admin1.png").ShowDialog();

                    config.WasRunAsAdmin = true;
                    config.Save();
                }
            }
            else
            {
                string text = Application.Current.Resources["Text_NoAdministration"] as string;
                new WindowAdministrator(text, "/Resources/Images/admin0.png").ShowDialog();
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            uc_iso.ExplorerB.Refresh();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
        }

        public void CheckWimlibOscdimgState()
        {
            // ===== WIMLIB =====
            bool wimlibState = wcwAppState.WimlibExists;

            uc_di.SetWimlibState(wimlibState);
            uc_adv.btnFCIW.IsEnabled = wimlibState;


            // ===== OSCDIMG =====
            bool oscdimgState = wcwAppState.OscdimgSysPath || wcwAppState.OscdimgProgPath;

            uc_wim.SetOscdimgState(oscdimgState);
        }

        public void OptWimlibBTN()
        {
            bool state = wcwAppState.WimlibExists;

            uc_di.btnOptimizeImage.IsEnabled = state;
        }

        public void IsoOscdimgBTN()
        {
            bool state = wcwAppState.OscdimgSysPath || wcwAppState.OscdimgProgPath;

            uc_wim.btnCII.IsEnabled = state;
        }

        private void SettingsProgram_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsWCW();
            settings.Owner = this;
            settings.Show();
        }

        private void HelpInfo_Click(object sender, RoutedEventArgs e)
        {
            var infohelp = new W_Help(Application.Current.Resources["Text_HelpProgram"] as string);
            infohelp.Owner = this;

            infohelp.Closed += (s, ss) =>
            {
                var main = System.Windows.Application.Current.MainWindow;

                if (main != null)
                {
                    main.Activate();
                    main.Focus();
                }
            };

            infohelp.Show();
        }

        private void AboutInfo_Click(object sender, RoutedEventArgs e)
        {
            var infoabout = new W_About(Application.Current.Resources["Text_AboutProgram"] as string);
            infoabout.Owner = this;

            infoabout.Closed += (s, ss) =>
            {
                var main = System.Windows.Application.Current.MainWindow;

                if (main != null)
                {
                    main.Activate();
                    main.Focus();
                }
            };

            infoabout.Show();
        }

        public void ActiveButtonOffline(bool enabled)
        { 
            uc_AllFPA.btnImageWim.IsEnabled = enabled;
        }

        public void ActivateButtonDI_ISO(bool disabled)
        {
            uc_di.btnConvertImage.IsEnabled = disabled;
            uc_di.btnOptimizeImage.IsEnabled = disabled;
            uc_di.btnUpdateIndex.IsEnabled = disabled;
            uc_di.SetIndexWimState(disabled);
            uc_iso.btnDeleteIsoCopy.IsEnabled = disabled;
        }

        public async void RefreshFeaturesOnline()
        {
            uc_AllFPA.Online();
        }

        public void UpdateJurnalText()
        {
            if (uc_AllFPA.MainContentControl.Content is UC_Features ucFeatures)
            {
                ucFeatures.UpdateJournalColors();
            }
        }

        public async Task RefreshFeatures()
        {
            if (uc_AllFPA.MainContentControl.Content is UC_Features ucFeatures)
            {
               await ucFeatures.RefreshFeatures();
            }
        }

        public void RefreshIndexUCWim()
        {
            uc_wim.LoadWimIndexes();
        }

        private void WorkDirectory_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).SelectAndInitWorkDirectory(isStartup: false);
        }

        public void UpdateCopyIsoButtonState()
        {
            bool isIsoMounted = !string.IsNullOrEmpty(wcwAppState.MountedIsoPath);
            string extractPath = Path.Combine(wcwAppState.ExtractIsoPath);
            bool hasExtractIso =
                Directory.Exists(extractPath) &&
                Directory.EnumerateFileSystemEntries(extractPath).Any();

            uc_iso.btnCopyISO.IsEnabled = isIsoMounted && !hasExtractIso;            
        }

        public void WD_UpdateButtonState()
        {
            uc_di.LoadWimIndexes();
            uc_di.SetIndexWimState(false);
            uc_di.btnOptimizeImage.IsEnabled = false;
            uc_di.btnUpdateIndex.IsEnabled = false;
            uc_di.btnDeleteIndex.IsEnabled = false;
            uc_di.btnConvertImage.IsEnabled = false;
            uc_di.btnOptimizeImage.IsEnabled = false;
            uc_di.txtProgress1.Text = Application.Current.Resources["Text_StatusOptimize1"] as string;
            uc_wim.SetIndexWimState(false);
            uc_wim.LoadWimIndexes();
            uc_wim.btnCII.IsEnabled = false;
        }

        public void CheckStatusIsoState()
        {
            string extractPath = Path.Combine(App.Config.WorkDirectory, "ExtractISO");
            string sourcesPath = Path.Combine(extractPath, "sources");
            string wimPath = Path.Combine(sourcesPath, "install.wim");
            string esdPath = Path.Combine(sourcesPath, "install.esd");

            if (File.Exists(wimPath))
            {
                ActivateWimMode();
                return;
            }

            if (File.Exists(esdPath))
            {
                ActivateEsdMode();
                return;
            }

            if (Directory.Exists(extractPath) &&
                Directory.EnumerateFileSystemEntries(extractPath).Any())
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var infoWindow = new WindowMessageInfo(
                        Application.Current.Resources["Text_WimNotFoundMessage"] as string
                    );

                    infoWindow.Owner = Application.Current.MainWindow;
                    infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    infoWindow.Show();
                }));
                ActivateNoMode();
                return;
            }
        }

        public void ActivateButtonAdv(bool enabled)
        {
            uc_adv.btnADI.IsEnabled = enabled;
        }

        private void ActivateWimMode()
        {
            uc_iso.btnOpenIso.IsEnabled = false;
            uc_iso.SetIsoUiStateB(true);
            uc_iso.RefreshExplorerB();
            UpdateCopyIsoButtonState();
            OptWimlibBTN();
            uc_di.SetIndexWimState(true);
            uc_di.btnUpdateIndex.IsEnabled = true;
            uc_di.btnDeleteIndex.IsEnabled = true;
            uc_di.btnConvertImage.IsEnabled = true;
            uc_di.btnDeleteIndex.IsEnabled = true;
            uc_di.btnInfoInstall.IsEnabled = true;
            uc_di.TextStatusWim.Text = "install.wim";
            uc_di.LoadWimIndexes();
            IsoOscdimgBTN();
            uc_wim.SetIndexWimState(true);
            uc_wim.LoadWimIndexes();
        }

        private void ActivateEsdMode()
        {
            uc_iso.btnOpenIso.IsEnabled = false;
            uc_iso.SetIsoUiStateB(true);
            uc_iso.RefreshExplorerB();
            UpdateCopyIsoButtonState();
            IsoOscdimgBTN();
            uc_di.SetIndexWimState(false);
            uc_di.btnConvertImage.IsEnabled = true;
            uc_di.btnDeleteIndex.IsEnabled = true;
            uc_di.btnInfoInstall.IsEnabled = true;
            uc_di.TextStatusWim.Text = "install.esd";

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var infoWindow = new WindowMessageInfo(
                    Application.Current.Resources["Text_DetectESD"] as string
                );

                infoWindow.Owner = Application.Current.MainWindow;
                infoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                infoWindow.Show();
            }));
        }

        private void ActivateNoMode()
        {
            uc_iso.btnOpenIso.IsEnabled = false;
            uc_iso.SetIsoUiStateB(true);
            uc_iso.RefreshExplorerB();
            UpdateCopyIsoButtonState();
        }
    }
}
