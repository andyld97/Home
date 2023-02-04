using Microsoft.Win32;
using System;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Home.Measure.Windows
{
    /// <summary>
    /// Capsels all methods available by .NET
    /// </summary>
    public static class NET
    {
        public static AddressResult DetermineIPAddress()
        {
            string returnAddress = string.Empty;
            string macAddress = string.Empty;

            try
            {
                // A device can also have multiple ip addresses, e.g. WLAN and LAN,
                // but it is required to find the "main" ip address (mostly LAN)

                // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection)
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces().OrderBy(p => p.Name).ToArray();

                foreach (NetworkInterface network in networkInterfaces)
                {
                    // Read the IP configuration for each network
                    IPInterfaceProperties properties = network.GetIPProperties();

                    string description = network.Description.ToLower();

                    if ((network.NetworkInterfaceType == NetworkInterfaceType.Ethernet || network.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) && network.OperationalStatus == OperationalStatus.Up && !description.Contains("virtual") && !description.Contains("pseudo"))
                    {
                        // Each network interface may have multiple IP addresses
                        foreach (IPAddressInformation address in properties.UnicastAddresses)
                        {
                            // We're only interested in IPv4 addresses for now
                            if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                                continue;

                            // Ignore loopback addresses (e.g., 127.0.0.1)
                            if (IPAddress.IsLoopback(address.Address))
                                continue;

                            returnAddress = address.Address.ToString();
                            macAddress = BitConverter.ToString(network.GetPhysicalAddress().GetAddressBytes()).Replace("-", ":");
                        }
                    }
                }
            }
            catch
            {

            }

            AddressResult result = new AddressResult();
            result.IpAddress = returnAddress;
            result.MacAddress = macAddress;
            return result;
        }


        /// <summary>
        /// Determines the OS-friendly name like "Windows 11 Pro Insider Preview (22H2)" with version!
        /// </summary>
        /// <param name="defaultValue">If there is an error the defaultValue will be returned</param>
        /// <returns></returns>
        public static string GetOsFriendlyName(string defaultValue)
        {
            try
            {
                var name = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                            select x.GetPropertyValue("Caption")).FirstOrDefault();

                // Helpful links for getting os versions and numbers correctly:
                // https://stackoverflow.com/questions/69885021/determine-the-windows-os-version-details
                // https://www.prugg.at/2019/09/09/properly-detect-windows-version-in-c-net-even-windows-10/

                string winVer = string.Empty;
                try
                {
                    if (Environment.OSVersion.Version.Major >= 10)
                    {
                        string HKLMWinNTCurrent = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";

                        if (Environment.OSVersion.Version.Build <= 19042)
                            winVer = Registry.GetValue(HKLMWinNTCurrent, "ReleaseId", "").ToString();
                        else
                            winVer = Registry.GetValue(HKLMWinNTCurrent, "DisplayVersion", "").ToString();
                    }
                }
                catch
                {
                    // ignore
                }

                if (name != null && !string.IsNullOrEmpty(name.ToString()))
                {
                    if (string.IsNullOrEmpty(winVer))
                        return name.ToString();

                    return $"{name} ({winVer})";
                }
            }
            catch
            { }

            return defaultValue;
        }

        /// <summary>
        /// Determine the MachineName with correct umlauts
        /// </summary>
        /// <returns></returns>
        public static string GetMachineName()
        {
            try
            {
                // https://stackoverflow.com/questions/1233217/difference-between-systeminformation-computername-environment-machinename-and
                // Hostname with umlauts won't work with Envoirnment.MachineName (e.g. BšRO-RECHNER)
                // Fallback with localhost/Hostname/then MachineName

                string name = System.Net.Dns.GetHostEntry("localhost").HostName;
                if (string.IsNullOrEmpty(name))
                    name = System.Net.Dns.GetHostName();

                if (!string.IsNullOrEmpty(name))
                    return name.ToUpper();
            }
            catch
            {

            }

            return Environment.MachineName;
        }


        /// <summary>
        /// Determines battery info related to the device!
        /// </summary>
        /// <param name="percentage"></param>
        /// <param name="isCharging"></param>
        /// <returns>false; if there is no battery available</returns>
        public static bool DetermineBatteryInfo(out int percentage, out bool isCharging)
        {
            var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;

            if (powerStatus.BatteryChargeStatus == System.Windows.Forms.BatteryChargeStatus.NoSystemBattery)
            {
                percentage = -1;
                isCharging = false;
                return false;
            }

            percentage = (int)Math.Round(powerStatus.BatteryLifePercent * 100, 0);
            isCharging = (powerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online || powerStatus.BatteryChargeStatus == System.Windows.Forms.BatteryChargeStatus.Charging);

            return true;
        }

        public static byte[] CaputreScreen(System.Windows.Forms.Screen screen)
        {
            byte[] result = null;

            try
            {
                using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(screen.Bounds.Width, screen.Bounds.Height))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                        {
                            g.CopyFromScreen(screen.Bounds.X, screen.Bounds.Y, 0, 0, bmp.Size);
                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            result = ms.ToArray();
                        }
                    }
                }
            }
            catch
            {
                // ToDo: Log?
            }

            return result;
        }


        public static byte[] CreateScreenshot(string fileName)
        {
            try
            {
                // ToDo: *** Respect dpi settings?
                double screenLeft = System.Windows.Forms.SystemInformation.VirtualScreen.Left; // SystemParameters.VirtualScreenLeft; [.net 4.8]
                double screenTop = System.Windows.Forms.SystemInformation.VirtualScreen.Top; // SystemParameters.VirtualScreenTop; [.net 4.8]
                double screenWidth = System.Windows.Forms.SystemInformation.VirtualScreen.Width; // SystemParameters.VirtualScreenWidth; [.net 4.8]
                double screenHeight = System.Windows.Forms.SystemInformation.VirtualScreen.Height; // SystemParameters.VirtualScreenHeight; [.net 4.8]

                using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)screenWidth, (int)screenHeight))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen((int)screenLeft, (int)screenTop, 0, 0, bmp.Size);
                        bmp.Save(fileName);
                    }
                }

                byte[] result = System.IO.File.ReadAllBytes(fileName);
                try
                {
                    System.IO.File.Delete(fileName);
                }
                catch
                {

                }


                return result;
                // ToDO: log
            }
            catch (Exception ex)
            {
                // ToDo: Log
             
            }

            return null;
        }
    }

    public struct AddressResult
    {
        public string IpAddress { get; set; }

        public string MacAddress { get; set; }
    }
}
