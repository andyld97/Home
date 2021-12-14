using Android.Content;
using Android.Net;
using Android.OS;
using Home.Model;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using static Android.Provider.Settings;

namespace Home.Service.Android.Helper
{
    /// <summary>
    /// The equivalent of Windows WMI 
    /// </summary>
    public static class DeviceInfoHelper
    {
        private static readonly DateTime dateTimeStarted = DateTime.Now;

        public static string ReadCPUName()
        {
            ProcessBuilder cmd;
            string result = "";

            try
            {
                string[] args = { "/system/bin/cat", "/proc/cpuinfo" };
                cmd = new ProcessBuilder(args);
                Java.Lang.Process process = cmd.Start();
                using (var inputStream = process.InputStream)
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = 0;
                    while ((bytesRead = inputStream.Read(buffer, bytesRead, buffer.Length)) != -1)
                        result += System.Text.Encoding.Default.GetString(buffer);
                }
            }
            catch
            { }

            if (string.IsNullOrEmpty(result))
                return string.Empty;

            string[] entries = result.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string entry in entries)
            {
                var subEntries = entry.Split(":", StringSplitOptions.RemoveEmptyEntries);
                if (subEntries.Length >= 1)
                {
                    var key = subEntries[0].Trim().ToLower();
                    var value = subEntries[1].Trim();

                    if (key == "hardware" || key == "model name")
                        return value;
                }
            }

            return result;
        }

        public static void ReadAndAssignMemoryInfo(Device device)
        {
            Java.Lang.ProcessBuilder cmd;
            string result = "";

            try
            {
                string[] args = { "/system/bin/cat", "/proc/meminfo" };
                cmd = new ProcessBuilder(args);

                Java.Lang.Process process = cmd.Start();
                using (var inputStream = process.InputStream)
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = 0;
                    while ((bytesRead = inputStream.Read(buffer, bytesRead, buffer.Length)) != -1)
                        result += System.Text.Encoding.Default.GetString(buffer);
                }
            }
            catch
            { }

            if (string.IsNullOrEmpty(result))
                return;

            string[] entries = result.Split(System.Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            if (entries.Length >= 2)
            {
                // MemTotal:        1530632 kB
                // MemFree:         351580 kB
                // ...
                string memTotal = entries[0].ToLower().Replace("memtotal:", string.Empty).Trim();
                string memFree = entries[1].ToLower().Replace("memfree:", string.Empty).Trim();

                double freeRam = ParseMemoryEntryInGB(memFree);
                device.Envoirnment.TotalRAM = ParseMemoryEntryInGB(memTotal);

                double totalGB = device.Envoirnment.TotalRAM;
                double usedGB = totalGB - freeRam;
                int percentage = (int)System.Math.Round((usedGB / totalGB) * 100);

                device.Envoirnment.FreeRAM = $"{System.Math.Round(usedGB, 2)} GB used ({percentage} %)";
            }
        }

        /// <summary>
        /// Takes e.g. 324234 Kb and converts it to GB
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ParseMemoryEntryInGB(string value)
        {
            // Supported units: b, kb, mb, gb, tb
            string[] entries = value.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (entries.Length == 2)
            {
                long lValue = long.Parse(entries[0]);

                int unit = 0;
                string unitValue = entries[1].Trim();
                if (unitValue == "b")
                    unit = 1;
                else if (unitValue == "kb")
                    unit = 2;
                else if (unitValue == "mb")
                    unit = 3;
                else if (unitValue == "gb")
                    unit = 4;
                else if (unitValue == "tb")
                    unit = 5;

                // Target unit is gb (4)
                int diff = 4 - unit;
                if (diff > 0)
                    return System.Math.Round(lValue / System.Math.Pow(1024.0, diff), 2);
                else
                    return System.Math.Round(lValue * System.Math.Pow(1024.0, diff), 2);
            }

            return -1;
        }


        public static string GetDeviceName(ContentResolver cr)
        {
            string userDeviceName = Global.GetString(cr, "device_name");
            if (userDeviceName == null)
                userDeviceName = Secure.GetString(cr, "bluetooth_name");

            return userDeviceName;
        }

        public static int GetNumberOfCores()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr1)
                return Runtime.GetRuntime().AvailableProcessors();

            return System.Environment.ProcessorCount;
        }

        public static string GetIpAddress(Context context)
        {
            ConnectivityManager cm = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
            var prop = cm.GetLinkProperties(cm.ActiveNetwork);

            List<string> ipAdresses = new List<string>();
            foreach (var ip in prop.LinkAddresses)
                ipAdresses.Add(ip.ToString());

            string ipV4 = ipAdresses.Where(i => i.Contains(".")).FirstOrDefault();
            if (!string.IsNullOrEmpty(ipV4))
                return ipV4;

            if (ipAdresses.Count > 0)
                return ipAdresses.FirstOrDefault();

            return string.Empty;
        }

        public static void RefreshDevice(this Device currentDevice, ContentResolver cr, Context context)
        {
            currentDevice.ServiceClientVersion = "vAndroid 0.0.1";
            currentDevice.Envoirnment.OSName = $"Android {Build.VERSION.Release}";
            currentDevice.Envoirnment.OSVersion = $"{currentDevice.Envoirnment.OSName} (Sec. Patch: {Build.VERSION.SecurityPatch}) ({System.Environment.OSVersion})";
            currentDevice.OS = Device.OSType.Android;
            currentDevice.Envoirnment.CPUCount = DeviceInfoHelper.GetNumberOfCores();
            currentDevice.Envoirnment.CPUName = DeviceInfoHelper.ReadCPUName();
            currentDevice.Envoirnment.Description = Build.Model;
            currentDevice.Envoirnment.DomainName = System.Environment.UserDomainName;
            currentDevice.Envoirnment.Is64BitOS = System.Environment.Is64BitOperatingSystem;
            currentDevice.Envoirnment.Product = Build.Product;
            currentDevice.Envoirnment.StartTimestamp = System.DateTime.Now;

            // Read and assign memory info
            DeviceInfoHelper.ReadAndAssignMemoryInfo(currentDevice);

            currentDevice.Envoirnment.Vendor = Build.Brand;
            currentDevice.Envoirnment.UserName = System.Environment.UserName;
            currentDevice.Envoirnment.Motherboard = Build.Board;
            currentDevice.Envoirnment.MachineName =
            currentDevice.Name = DeviceInfoHelper.GetDeviceName(cr);
            currentDevice.IP = DeviceInfoHelper.GetIpAddress(context);
            currentDevice.Envoirnment.RunningTime = DateTime.Now.Subtract(dateTimeStarted);
        }
    }
}

/*
 * 
 *  string arch = Java.Lang.JavaSystem.GetProperty("os.arch");

            tv.Text = "***** DEVICE Information *****" + "\n";
            tv.Append("Model: " + Build.Model + "\n");
            tv.Append("Board: " + Build.Board + "\n");
            tv.Append("Brand: " + Build.Brand + "\n");
            tv.Append("Manufacturer: " + Build.Manufacturer + "\n");
            tv.Append("Device: " + Build.Device + "\n");
            tv.Append("Product: " + Build.Product + "\n");
            tv.Append("TAGS: " + Build.Tags + "\n");
            tv.Append("Serial: " + Build.Serial + "\n");

            tv.Append("\n" + "***** SOC *****" + "\n");
            tv.Append("Hardware: " + Build.Hardware + "\n");
            tv.Append("Number of cores: " +  DeviceInfoHelper. GetNumberOfCores() + "\n");
            tv.Append("Architecture: " + arch + "\n");

            tv.Append("\n" + "***** CPU Info *****" + "\n");
            tv.Append(DeviceInfoHelper.ReadCPUName() + "\n");

            tv.Append("\n" + "***** Memory Info *****" + "\n");
            tv.Append(DeviceInfoHelper.ReadMemoryInfo() + "\n");

            tv.Append("\n" + "***** OS Information *****" + "\n");
            tv.Append("Build release: " + Build.VERSION.Release + "\n");
            tv.Append("Incremental release: " + Build.VERSION.Incremental + "\n");
            tv.Append("Base OS: " + Build.VERSION.BaseOs + "\n");
            tv.Append("CODE Name: " + Build.VERSION.Codename + "\n");
            tv.Append("Security patch: " + Build.VERSION.SecurityPatch + "\n");
            tv.Append("Preview SDK: " + Build.VERSION.PreviewSdkInt + "\n");
            tv.Append("SDK/API version: " + Build.VERSION.SdkInt + "\n");
            tv.Append("Display build: " + Build.Display + "\n");
            tv.Append("Finger print: " + Build.Fingerprint + "\n");
            tv.Append("Build ID: " + Build.Id + "\n");

            SimpleDateFormat sdf = new SimpleDateFormat("MMMM d, yyyy 'at' h:mm a");
            string date = sdf.Format(Build.Time);

            tv.Append("Build Time: " + date + "\n");
            tv.Append("Build Type: " + Build.Type + "\n");
            tv.Append("Build User: " + Build.User + "\n");
            tv.Append("Bootloader: " + Build.Bootloader + "\n");
            tv.Append("Kernel version: " + Java.Lang.JavaSystem.GetProperty("os.version") + "\n");
 * */