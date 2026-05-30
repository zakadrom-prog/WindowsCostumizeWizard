namespace WindowsCostumizeWizard
{
    public static class wcwAppState
    {
        // ISO
        public static string MountedIsoPath { get; set; }
        // Робоча директорія WCW
        public static string WorkDirectory { get; set; }
        // Чи є wimlib
        public static bool WimlibExists { get; set; }
        // Повний шлях до MountWIM
        public static string MountWimPath { get; set; }
        // Чи змонтований install.wim
        public static bool IsWimMounted { get; set; }
        // Повний шлях до ExtractISO
        public static string ExtractIsoPath { get; set; }
        // Індекс змонтованого образу
        public static int MountIndex { get; set; } = -1;
        public static bool OscdimgSysPath { get; set; }
        public static bool OscdimgProgPath { get; set; }
    }
}