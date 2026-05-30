using System.Windows;
using System.Windows.Input;

namespace WindowsCostumizeWizard.WindowInfo
{
    public partial class WindowMessageInfo : Window
    {
        public string InfoText { get; set; }

        public WindowMessageInfo(string text = "")
        {
            InitializeComponent();

            InfoText = text.Replace("\\n", "\n");
            DataContext = this;
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