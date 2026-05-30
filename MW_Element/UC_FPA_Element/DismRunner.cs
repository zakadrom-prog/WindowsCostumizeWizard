using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WindowsCostumizeWizard.MW_Element.UC_FPA_Element
{
    public class DismRunner
    {
        private readonly TextBox _logTextBox;
        private readonly string _featureName;
        private readonly bool _enable;

        public DismRunner(string featureName, bool enable, TextBox logTextBox)
        {
            _featureName = featureName;
            _enable = enable;
            _logTextBox = logTextBox;
        }

        public void Run()
        {
            Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = _enable
                            ? $"/Online /Enable-Feature /FeatureName:\"{_featureName}\" /NoRestart"
                            : $"/Online /Disable-Feature /FeatureName:\"{_featureName}\" /NoRestart",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = new Process { StartInfo = psi })
                    {
                        process.Start();

                        // Читаємо стандартний вивід
                        while (!process.StandardOutput.EndOfStream)
                        {
                            string line = process.StandardOutput.ReadLine();
                            ProcessLine(line);
                        }

                        // Читаємо помилки
                        while (!process.StandardError.EndOfStream)
                        {
                            string err = process.StandardError.ReadLine();
                            ProcessLine(err);
                        }

                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"Error running DISM for {_featureName}: {ex.Message}");
                }
            });
        }

        private void ProcessLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            // Об’єднуємо прогрес у один рядок
            if (line.StartsWith("[===="))
            {
                line = "[==========================100.0%==========================]";
            }

            AppendLog($"{_featureName}\n{line}\n");
        }

        private void AppendLog(string text)
        {
            if (_logTextBox == null) return;

            _logTextBox.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                _logTextBox.AppendText(text + "\n");
                _logTextBox.ScrollToEnd();
            }));
        }
    }
}