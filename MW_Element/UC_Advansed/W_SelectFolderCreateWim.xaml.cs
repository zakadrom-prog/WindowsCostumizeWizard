using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace WindowsCostumizeWizard.MW_Element.UC_Advansed
{
    public partial class W_SelectFolderCreateWim : Window
    {
        public string InfoText { get; set; }

        public W_SelectFolderCreateWim(string text = "")
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

        private void OpenFolderCreateWim_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                CreateImageWim(dialog.FileName);
                Close();
            }
        }

        private void CreateImageWim(string folderPath)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string wimlibPath = Path.Combine(baseDir, "wimlib", "wimlib-imagex.exe");
                string outputWim = Path.Combine(folderPath, "install.wim");

                if (!File.Exists(wimlibPath))
                    throw new FileNotFoundException("wimlib-imagex.exe not found", wimlibPath);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = wimlibPath,
                    Arguments = $"capture C:\\ \"{outputWim}\" \"Windows Custom\" --compress=LZX --snapshot",

                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
