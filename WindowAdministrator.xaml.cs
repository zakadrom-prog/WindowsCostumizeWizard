using System.Windows;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace WindowsCostumizeWizard
{
    public partial class WindowAdministrator : Window
    {
        public string InfoText { get; set; }
        public string ImageSource { get; set; }

        public WindowAdministrator(string text, string image)
        {
            InitializeComponent();
            InfoText = text.Replace("\\n", "\n");
            ImageSource = image;
            DataContext = this;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
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
