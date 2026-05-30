using System.Dynamic;
using System.Windows;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace WindowsCostumizeWizard.MW_Element.UC_FPA_Element
{
    public partial class W_Help : Window
    {
        public string InfoHelp {  set; get; }
        public W_Help(string text  = "")
        {
            InitializeComponent();
            InfoHelp = text.Replace("\\n", "\n");
            DataContext = this;
        }

        private void CloseWindowHelp_Click(object sender, RoutedEventArgs e)
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
