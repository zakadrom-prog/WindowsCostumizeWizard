using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace WindowsCostumizeWizard
{
    public partial class UpdateInfo : Window
    {
        public UpdateInfo()
        {
            InitializeComponent();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AppConfig config = AppConfig.Load();
            config.WindowUpdate = false;
            config.Save();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AppConfig config = AppConfig.Load();
            config.WindowUpdate = true;
            config.Save();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });

            e.Handled = true;
        }

        private void CloseWindowInfo_Click(object sender, RoutedEventArgs e)
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
