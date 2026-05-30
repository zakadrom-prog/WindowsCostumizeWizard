using System;
using System.Windows;

namespace WindowsCostumizeWizard.WindowInfo
{
    public partial class WindowProgressBlock : Window
    {
        public WindowProgressBlock(string text)
        {
            InitializeComponent();
            DataContext = text;
        }

        public void UpdateText(string text)
        {
            DataContext = text;
        }
    }
}
