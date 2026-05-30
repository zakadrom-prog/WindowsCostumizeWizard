using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace WindowsCostumizeWizard.MW_Element
{
    public partial class W_WindowEndMount : Window, INotifyPropertyChanged
    {
        private string _infoEndMount;
        public string InfoEndMount
        {
            get => _infoEndMount;
            set
            {
                if (_infoEndMount != value)
                {
                    _infoEndMount = value;
                    OnPropertyChanged(nameof(InfoEndMount));
                }
            }
        }

        public W_WindowEndMount()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void CloseWindowInfo_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}