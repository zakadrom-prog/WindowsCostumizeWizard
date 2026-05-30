using System.Windows;
using System.Windows.Input;

namespace WindowsCostumizeWizard.Windowinfo
{
    public partial class WindowProgramInfo : Window
    {
        public string InfoText { get; set; }

        public WindowProgramInfo(string text = "")
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
