using System;

namespace Home.Data
{
    public static class Consts
    {
        public static readonly string SCREENSHOT_DATE_FILE_FORMAT = "ddMMyyyy-HHmmss";
        public static readonly int API_PORT = 5556;

        public static readonly string[] TEXT_EXTENSIONS = new string[] { ".txt", ".ini", ".log", ".json", ".xml", ".yaml", ".cs", ".vb", ".csproj", ".vbproj", ".sln", ".js", ".css", ".vbs", ".php", ".py", ".cpp", ".c", ".h" };
        public static readonly string[] HTML_EXTENSIONS = new string[] { ".html", ".htm" };
        public static readonly string[] IMG_EXTENSIONS = new string[] { ".jpg", ".jpeg", ".jpe", ".jif", ".jfif", ".jfi", ".png", ".gif", ".tiff", ".tif", ".bmp", ".jp2", ".j2k", ".jpf", ".jpx", ".jpm", ".mj2", ".heif", ".heic" };

        public static readonly DateTime ReleaseDate = new DateTime(2023, 7, 2, 11, 34, 0);

        public const string HomeServiceWindowsClientVersion = "1.2.4";

        public const string HomeServiceLinuxClientVersion = "1.1.4";

        public const string HomeServiceLegacyClientVersion = "1.1.0";      

        public const string HomeServiceAndroidClientVersion = "1.1.3";
    }
}