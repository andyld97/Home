using System;
using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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

        /// <summary>
        /// Gets the descriptions of an enum value (https://stackoverflow.com/a/1415187/6237448)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }

        public static string BuildSHA1Hash(this string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        public static ulong ParseDFEntry(string entry)
        {
            // entry might be 3,3T, 449G, 500M or 123k
            string value = entry.Substring(0, entry.Length - 1);
            int factor = 0;

            if (entry.EndsWith("k"))
                factor = 1;
            else if (entry.EndsWith("M"))
                factor = 2;
            else if (entry.EndsWith("G"))
                factor = 3;
            else if (entry.EndsWith("T"))
                factor = 4;

            if (double.TryParse(value, out double entryValue))
                return (ulong)Math.Round(entryValue * Math.Pow(1024, factor));

            return 0;
        }
    }
}