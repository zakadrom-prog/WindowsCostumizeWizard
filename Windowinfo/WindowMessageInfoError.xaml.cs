using System.Windows;
using System.Windows.Input;

namespace WindowsCostumizeWizard.Windowinfo
{
    public partial class WindowMessageInfoError : Window
    {
        public string InfoText { get; set; }
        public WindowMessageInfoError(string text = "")
        {
            InitializeComponent();
            {
                InitializeComponent();

                InfoText = text.Replace("\\n", "\n");
                DataContext = this;
            }
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
