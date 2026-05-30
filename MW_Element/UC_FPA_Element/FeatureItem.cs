using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace WindowsCostumizeWizard.MW_Element.UC_FPA_Element
{
    public class FeatureItem : INotifyPropertyChanged
    {
        private bool _isChecked;
        private bool _isEnabledCheck = true;
        private string _state;

        // Для NetFx3 ручного встановлення
        public string CabPath { get; set; }

        public string Name { get; set; }

        public bool IsPayloadRemoved => Name?.Equals("NetFx3", StringComparison.OrdinalIgnoreCase) == true
                    &&
                    State?.Equals("Disabled with Payload Removed", StringComparison.OrdinalIgnoreCase) == true;

        public string State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged(nameof(State));
                OnPropertyChanged(nameof(Foreground));
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }

        public bool IsEnabledCheck
        {
            get => _isEnabledCheck;
            set
            {
                _isEnabledCheck = value;
                OnPropertyChanged(nameof(IsEnabledCheck));
                OnPropertyChanged(nameof(TextForeground));
            }
        }

        public ObservableCollection<FeatureItem> Children { get; set; } = new ObservableCollection<FeatureItem>();

        public Brush Foreground
        {
            get
            {
                if (string.Equals(State, "Enabled", System.StringComparison.OrdinalIgnoreCase))
                    return Brushes.Green;
                else if (string.Equals(State, "Disabled", System.StringComparison.OrdinalIgnoreCase))
                    return Brushes.Gray;
                else
                    return Brushes.White;
            }
        }

        public Brush TextForeground
        {
            get
            {
                if (!IsEnabledCheck)
                    return Brushes.DarkGray;

                return Foreground;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}