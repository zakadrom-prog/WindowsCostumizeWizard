using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WindowsCostumizeWizard
{
    public partial class SettingsWCW : Window
    {
        private bool _isInitializing;
        public SettingsWCW()
        {
            InitializeComponent();
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            _isInitializing = true;

            var cfg = App.Config;

            // Мова
            LanguageComboBox.SelectedIndex =
                cfg.Language == "en" ? 1 : 0;

            // Тема
            ThemeComboBox.SelectedIndex =
                cfg.Theme == "Dark" ? 0 : 1;

            _isInitializing = false;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            string lang = LanguageComboBox.SelectedIndex == 1
                ? "en"
                : "ua";

            ((App)Application.Current).UpdateConfig(lang, App.Config.Theme);
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            string theme = ThemeComboBox.SelectedIndex == 1
                ? "Light"
                : "Dark";

            ((App)Application.Current).UpdateConfig(App.Config.Language, theme);

            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.UpdateJurnalText();
            }
        }
    }
}
