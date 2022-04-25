using System;
using static Home.Model.Device;

namespace Home.Data.Helper
{
    public static class GeneralHelper
    {
        /// <summary>
        /// Determines if the os value is a Windows version
        /// </summary>
        /// <param name="value"></param>
        /// <param name="includeLegacyVersions">If true, Windows XP and VISTA are also considered</param>
        /// <returns></returns>
        public static bool IsWindows(this OSType value, bool includeLegacyVersions)
        {
            if (includeLegacyVersions && value.IsWindowsLegacy())
                return true;

            return (value == OSType.Windows7 || value == OSType.Windows8 || value == OSType.Windows10 || value == OSType.Windows11);
        }

        /// <summary>
        /// Determines if the os value is a legacy version of Windows
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true for XP and VISTA</returns>
        public static bool IsWindowsLegacy(this OSType value)
        {
            return (value == OSType.WindowsXP || value == OSType.WindowsaVista);
        }

        /// <summary>
        /// Determines if the os value is Android
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsAndroid(this OSType value)
        {
            return (value == OSType.Android);
        }

        /// <summary>
        /// Determines if the os value is a Linux Distro
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsLinux(this OSType value)
        {
            // Theoretically Android OS is also based on Linux, but in this case it doesn't count as Linux
            return (value == OSType.Linux || value == OSType.LinuxUbuntu || value == OSType.LinuxMint);
        }
    }
}