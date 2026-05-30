using System.Collections.Generic;
using System.IO;

namespace WindowsCostumizeWizard.MW_Element
{
    public class ExplorerNavigation
    {
        private Stack<string> _history;

        public string CurrentPath { get; private set; }

        public bool CanGoBack
        {
            get { return _history.Count > 0; }
        }

        public ExplorerNavigation()
        {
            _history = new Stack<string>();
        }

        // Перехід в нову папку
        public void Navigate(string path)
        {
            if (!Directory.Exists(path))
                return;

            if (CurrentPath != null)
                _history.Push(CurrentPath);

            CurrentPath = path;
        }

        // Назад
        public void GoBack()
        {
            if (!CanGoBack)
                return;

            CurrentPath = _history.Pop();
        }

        // Скидання до кореня
        public void Reset(string path)
        {
            _history.Clear();
            CurrentPath = path;
        }
    }
}
