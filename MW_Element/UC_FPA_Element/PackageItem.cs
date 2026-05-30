using System.ComponentModel;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace WindowsCostumizeWizard.MW_Element.UC_FPA_Element
{
    public class PackageItem : INotifyPropertyChanged
    {
        private bool isSelected;
        private bool isChecked;

        public string Identity { get; set; }
        public string State { get; set; }
        public string ReleaseType { get; set; }
        public string InstallTime { get; set; }
        public string Number { get; set; }

        // Виділення рядка
        public bool IsSelected
        {
            get => isSelected;
            set { isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        // Стан чекбокса
        public bool IsChecked
        {
            get => isChecked;
            set { isChecked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}