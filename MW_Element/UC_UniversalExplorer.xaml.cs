using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WindowsCostumizeWizard.MW_Element
{
    public partial class UC_UniversalExplorer : UserControl
    {
        // CurrentPath як DependencyProperty
        public static readonly DependencyProperty CurrentPathProperty =
            DependencyProperty.Register(
                "CurrentPath",
                typeof(string),
                typeof(UC_UniversalExplorer),
                new PropertyMetadata(null, OnPathChanged));

        public string CurrentPath
        {
            get { return (string)GetValue(CurrentPathProperty); }
            set { SetValue(CurrentPathProperty, value); }
        }

        private readonly ObservableCollection<ExplorerItem> _items = new ObservableCollection<ExplorerItem>();
        private ICollectionView _collectionView;

        public UC_UniversalExplorer()
        {
            InitializeComponent();
            List.ItemsSource = _items;

            _collectionView = CollectionViewSource.GetDefaultView(_items);
            _collectionView.Filter = FilterItems;

            List.MouseDoubleClick += List_MouseDoubleClick;
            this.PreviewKeyDown += UC_UniversalExplorer_PreviewKeyDown;
        }

        private void UC_UniversalExplorer_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Back) // клавіша Backspace
            {
                // Логіка повернення на папку вище прямо тут
                if (!string.IsNullOrEmpty(CurrentPath))
                {
                    DirectoryInfo parent = Directory.GetParent(CurrentPath);
                    if (parent != null && Directory.Exists(parent.FullName))
                    {
                        CurrentPath = parent.FullName; // автоматично завантажить папку
                    }
                }

                e.Handled = true; // щоб подія не пішла далі
            }
        }

        // Refresh переглядача
        public void Refresh()
        {
            if (!string.IsNullOrEmpty(CurrentPath) && Directory.Exists(CurrentPath))
            {
                LoadDirectory(CurrentPath);
            }
        }

        // Очищення списку
        public void ClearItems()
        {
            _items.Clear();
        }

        // Фільтр для пошуку
        private bool FilterItems(object obj)
        {
            ExplorerItem item = obj as ExplorerItem;
            if (item == null) return false;

            if (item.IsParent) return true;

            if (string.IsNullOrWhiteSpace(SearchBox.Text)) return true;

            return item.Name.ToLower().Contains(SearchBox.Text.ToLower());
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_collectionView != null)
                _collectionView.Refresh();
        }

        // Викликається при зміні CurrentPath
        private static void OnPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UC_UniversalExplorer explorer = d as UC_UniversalExplorer;
            if (explorer != null)
            {
                string newPath = e.NewValue as string;
                explorer.LoadDirectory(newPath);
            }
        }

        private void LoadDirectory(string path)
        {
            _items.Clear();

            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return;

            try
            {
                // Додати ".." для повернення на рівень вище
                DirectoryInfo parent = Directory.GetParent(path);
                if (parent != null)
                {
                    _items.Add(new ExplorerItem
                    {
                        Name = "📂 ..",
                        FullPath = parent.FullName,
                        IsDirectory = true,
                        Icon = GetIcon(parent.FullName, true),
                        IsParent = true
                    });
                }

                foreach (string dir in Directory.GetDirectories(path))
                {
                    _items.Add(new ExplorerItem
                    {
                        Name = Path.GetFileName(dir),
                        FullPath = dir,
                        IsDirectory = true,
                        Icon = GetIcon(dir, true)
                    });
                }

                foreach (string file in Directory.GetFiles(path))
                {
                    _items.Add(new ExplorerItem
                    {
                        Name = Path.GetFileName(file),
                        FullPath = file,
                        IsDirectory = false,
                        Icon = GetIcon(file, false)
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Explorer error");
            }

            SearchBox.Text = string.Empty;
            if (_collectionView != null)
                _collectionView.Refresh();
        }

        private void List_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ExplorerItem item = List.SelectedItem as ExplorerItem;
            if (item != null && item.IsDirectory)
            {
                CurrentPath = item.FullPath;
            }
        }

        #region SHGetFileInfo для іконок

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_SMALLICON = 0x1;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        private BitmapSource GetIcon(string path, bool isFolder)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            uint flags = SHGFI_ICON | SHGFI_SMALLICON;
            uint attribs = isFolder ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;

            IntPtr hImg = SHGetFileInfo(path, attribs, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            if (shinfo.hIcon == IntPtr.Zero) return null;

            BitmapSource img = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                shinfo.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(16, 16));

            DestroyIcon(shinfo.hIcon);
            return img;
        }

        #endregion
    }

    public class ExplorerItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsParent { get; set; }
        public BitmapSource Icon { get; set; }
    }
}