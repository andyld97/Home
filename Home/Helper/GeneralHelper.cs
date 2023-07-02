using Home.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Home.Helper
{
    public static class GeneralHelper
    {
        /// <summary>
        /// Opens the default system browser with the requested uri
        /// https://stackoverflow.com/questions/4580263/how-to-open-in-default-browser-in-c-sharp/67838115#67838115
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static bool OpenUri(Uri uri)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"\"{uri}\"");
                return true;
            }
            catch (Exception)
            {
                // ignore 
                return false;
            }
        }

        /// <summary>
        /// Counts all warnings of the given device
        /// </summary>
        /// <param name="device">The given device</param>
        /// <returns></returns>
        public static int CountWarnings(this Device device)
        {
            int warnings = 0;
            if (device.BatteryWarning != null)
                warnings++;

            warnings += device.StorageWarnings.Count;

            return warnings;
        }
    }
}
