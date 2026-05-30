using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsCostumizeWizard.MW_Element.UC_FPA_Element
{
    public class AppxItem : INotifyPropertyChanged
    {
        private bool isSelected;
        private bool isChecked;

        public string PackageName { get; set; }
        public string Architecture { get; set; }
        public string Version { get; set; }
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
