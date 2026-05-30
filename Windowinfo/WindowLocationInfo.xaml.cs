using System.Windows;
using System.Windows.Input;

namespace WindowsCostumizeWizard.WindowInfo
{
    public partial class WindowLocationInfo : Window
    {
        public string InfoText { get; set; }

        public WindowLocationInfo(string text = "")
        {
            InitializeComponent();

            InfoText = text.Replace("\\n", "\n");
            DataContext = this;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
