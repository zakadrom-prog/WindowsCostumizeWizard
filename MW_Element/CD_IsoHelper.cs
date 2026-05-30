using Shell32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Shell32Shell = Shell32.Shell;

namespace WindowsCostumizeWizard.MW_Element
{
    internal class CD_IsoHelper
    {
        public static void CopyIsoSystemShell0(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(sourceDir))
                throw new DirectoryNotFoundException("Змонтований диск не знайдено.");

            Directory.CreateDirectory(targetDir);

            Shell32Shell shell = new Shell32Shell();
            Folder sourceFolder = shell.NameSpace(sourceDir);
            Folder destFolder = shell.NameSpace(targetDir);

            if (sourceFolder == null || destFolder == null)
                throw new Exception("Не вдалося отримати доступ до папок через Shell.");

            FolderItems items = sourceFolder.Items();

            int FOF_SHOWPROGRESS = 0x00000010; // показ прогресу
            destFolder.CopyHere(items, FOF_SHOWPROGRESS);
        }

        public static bool CopyIsoSystemShell(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(sourceDir))
                throw new DirectoryNotFoundException("Змонтований диск не знайдено.");

            Directory.CreateDirectory(targetDir);

            Shell32Shell shell = new Shell32Shell();
            Folder sourceFolder = shell.NameSpace(sourceDir);
            Folder destFolder = shell.NameSpace(targetDir);

            if (sourceFolder == null || destFolder == null)
                throw new Exception("Не вдалося отримати доступ до папок через Shell.");

            FolderItems items = sourceFolder.Items();

            int FOF_SHOWPROGRESS = 0x00000010; // показ прогресу
            destFolder.CopyHere(items, FOF_SHOWPROGRESS);

            // Проста перевірка: чи папка містить файли після копіювання
            return Directory.Exists(targetDir) && Directory.GetFiles(targetDir).Length > 0;
        }

        // Видалення через Shell (новий метод)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        private const uint FO_DELETE = 0x0003;
        private const ushort FOF_ALLOWUNDO = 0x0040;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const ushort FOF_SIMPLEPROGRESS = 0x0100;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        public static void DeleteExtractIsoShell0(string targetDir)
        {
            if (!Directory.Exists(targetDir))
                return;

            // Отримуємо всі елементи всередині папки
            string[] entries = Directory.GetFileSystemEntries(targetDir, "*", SearchOption.TopDirectoryOnly);

            if (entries.Length == 0)
                return;

            // Формуємо список рядком, через \0, і завершуємо подвійним \0
            string from = string.Join("\0", entries) + "\0\0";

            SHFILEOPSTRUCT fileOp = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = from,
                fFlags = FOF_ALLOWUNDO | FOF_SIMPLEPROGRESS, // зелений progress + відправка у кошик
                hwnd = IntPtr.Zero,
                lpszProgressTitle = "Видалення файлів ExtractISO..."
            };

            SHFileOperation(ref fileOp);

            // Після виконання — папка targetDir порожня, сама папка не видаляється
        }

        public static bool DeleteExtractIsoShell(string targetDir)
        {
            if (!Directory.Exists(targetDir))
                return false;

            string[] entries = Directory.GetFileSystemEntries(targetDir, "*", SearchOption.TopDirectoryOnly);
            if (entries.Length == 0)
                return false;

            string from = string.Join("\0", entries) + "\0\0";

            SHFILEOPSTRUCT fileOp = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = from,
                fFlags = FOF_ALLOWUNDO | FOF_SIMPLEPROGRESS,
                hwnd = IntPtr.Zero,
                lpszProgressTitle = "Видалення файлів ExtractISO..."
            };

            SHFileOperation(ref fileOp);

            // якщо користувач скасував операцію
            if (fileOp.fAnyOperationsAborted)
                return false;

            return true;
        }
    }
}