using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WindowsCostumizeWizard.MW_Element
{
    public static class IsoMounter
    {
        public static bool Mount(string isoPath)
        {
            if (!File.Exists(isoPath))
                return false;

            Unmount();

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"Mount-DiskImage -ImagePath \"{isoPath}\"",
                Verb = "runas",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(psi)?.WaitForExit();

            return RefreshMountedPath();
        }

        public static void Unmount0()
        {
            if (string.IsNullOrEmpty(wcwAppState.MountedIsoPath))
                return;

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "Get-DiskImage | Where-Object {$_.Attached -eq $true} | Dismount-DiskImage",
                Verb = "runas",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(psi)?.WaitForExit();
            wcwAppState.MountedIsoPath = null;
        }

        public static bool Unmount()
        {
            // Приклад: змінна App.MountedIsoPath = "K:\"
            string drivePath = wcwAppState.MountedIsoPath;

            if (string.IsNullOrEmpty(drivePath) || drivePath.Length != 3 || drivePath[1] != ':' || drivePath[2] != '\\')
                return false; // некоректна буква диска

            // PowerShell команда для демонтажу диска по букві
            string psCommand = $@"$disk = Get-Volume -DriveLetter '{drivePath[0]}' | Get-DiskImage
                               if ($disk -and $disk.Attached) {{ Dismount-DiskImage -ImagePath $disk.ImagePath }}";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = psCommand,
                Verb = "runas",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            // Виконуємо PowerShell і чекаємо завершення демонтажу
            Process.Start(psi)?.WaitForExit();

            // Очищаємо змінну, бо диск більше не змонтований
            wcwAppState.MountedIsoPath = null;

            return true;
        }

        private static bool RefreshMountedPath()
        {
            // Шукаємо перший диск типу CDRom, який готовий
            var drive = DriveInfo.GetDrives()
                .FirstOrDefault(d => d.DriveType == DriveType.CDRom && d.IsReady);

            if (drive == null)
            {
                // Якщо диска немає, очищаємо дані
                wcwAppState.MountedIsoPath = null;
                return false;
            }

            // Якщо диск знайдено, записуємо його у wcwAppState
            wcwAppState.MountedIsoPath = drive.RootDirectory.FullName;
            return true;
        }
    }
}