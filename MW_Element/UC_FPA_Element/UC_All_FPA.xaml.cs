using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WindowsCostumizeWizard.MW_Element.UC_FPA_Element
{
    public partial class UC_All_FPA : UserControl
    {
        private UC_Features _ucFeatures;
        private UC_Packages _ucPackages;
        private UC_Appx _ucAppx;

        public enum FpaMode
        {
            Online,
            Offline
        }

        public async void ReInit()
        {
            _ucFeatures = new UC_Features(FpaState.Mode);
            _ucPackages = new UC_Packages(FpaState.Mode);
            _ucAppx = new UC_Appx(FpaState.Mode);
            MainContentControl.Content = _ucFeatures;
            await _ucFeatures.InitAsync();
            SetButtonsState(features: true);
        }

        public UC_All_FPA()
        {
            InitializeComponent();

            _ucFeatures = new UC_Features(FpaState.Mode);
            _ucPackages = new UC_Packages(FpaState.Mode);
            _ucAppx = new UC_Appx(FpaState.Mode);
            MainContentControl.Content = _ucFeatures;

            Loaded += async (_, __) =>
            {
                await _ucFeatures.InitAsync();
            };
        }

        private async void LoadFeatures_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = _ucFeatures;
            await _ucFeatures.InitAsync();
            SetButtonsState(features: true);
        }

        private async void LoadPackages_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = _ucPackages;
            await _ucPackages.InitAsync();
            SetButtonsState(packages: true);
        }

        private async void LoadAppx_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = _ucAppx;
            await _ucAppx.InitAsync();
            SetButtonsState(appx: true);
        }

        private void Online_Click(object sender, RoutedEventArgs e)
        {
            Online();
        }

        public void Online()
        {
            FpaState.Mode = FpaMode.Online;
            ReInit();
        }

        private void Offline_Click(object sender, RoutedEventArgs e)
        {
            Offline();
        }

        public void Offline()
        {
            FpaState.Mode = FpaMode.Offline;
            ReInit();
        }

        private void SetButtonsState(bool features = false, bool packages = false, bool appx = false)
        {
            btnLoadFeatures.IsEnabled = !features;
            btnLoadPackages.IsEnabled = !packages;
            btnLoadAppx.IsEnabled = !appx;
        }

        public void DisableFPA_btn(bool enabled)
        {
            this.IsEnabled = enabled;
            _ucFeatures?.DisableUI(enabled);
        }
    }
}