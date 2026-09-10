using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WindowsCostumizeWizard.MW_Element
{
    public static class IsoMounter
    {
        // Повний шлях до Windows PowerShell, PATH не використовується
        private static string PowerShellPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "System32",
                "WindowsPowerShell",
                "v1.0",
                "powershell.exe");

        public static bool Mount(string isoPath)
        {
            if (!File.Exists(isoPath))
                return false;

            if (!File.Exists(PowerShellPath))
            {
                System.Windows.MessageBox.Show(
                    "Збій у системі.\n\n" +
                    "Не знайдено Windows PowerShell:\n" +
                    PowerShellPath,
                    "Помилка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);

                return false;
            }

            Unmount();

            var psi = new ProcessStartInfo
            {
                FileName = PowerShellPath,
                Arguments = $"Mount-DiskImage -ImagePath \"{isoPath}\"",
                Verb = "runas",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(psi)?.WaitForExit();

            return RefreshMountedPath();
        }

        public static bool Unmount()
        {
            string drivePath = wcwAppState.MountedIsoPath;

            if (string.IsNullOrEmpty(drivePath) || drivePath.Length != 3 || drivePath[1] != ':' || drivePath[2] != '\\')
                return false;

            string psCommand = $@"$disk = Get-Volume -DriveLetter '{drivePath[0]}' | Get-DiskImage
                               if ($disk -and $disk.Attached) {{ Dismount-DiskImage -ImagePath $disk.ImagePath }}";

            var psi = new ProcessStartInfo
            {
                FileName = PowerShellPath,
                Arguments = psCommand,
                Verb = "runas",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(psi)?.WaitForExit();

            wcwAppState.MountedIsoPath = null;

            return true;
        }

        private static bool RefreshMountedPath()
        {
            var drive = DriveInfo.GetDrives()
                .FirstOrDefault(d => d.DriveType == DriveType.CDRom && d.IsReady);

            if (drive == null)
            {
                wcwAppState.MountedIsoPath = null;
                return false;
            }

            wcwAppState.MountedIsoPath = drive.RootDirectory.FullName;
            return true;
        }
    }
}