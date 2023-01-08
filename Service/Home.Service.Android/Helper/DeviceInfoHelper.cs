using Android.Content;
using Android.Net;
using Android.OS;
using Home.Data.Helper;
using Home.Model;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Android.Provider.Settings;
using Exception = Java.Lang.Exception;
using A = Android;

namespace Home.Service.Android.Helper
{
    /// <summary>
    /// The equivalent of Windows WMI 
    /// </summary>
    public static class DeviceInfoHelper
    {
        private static readonly DateTime dateTimeStarted = DateTime.Now;

        private static string ExecuteProcess(string[] args)
        {
            ProcessBuilder cmd = new ProcessBuilder(args);
            string result = string.Empty;

            try
            {
                var proc = cmd.Start();
                using (var inputStream = proc.InputStream)
                using (StreamReader sr = new StreamReader(inputStream))
                {
                    result = sr.ReadToEnd();
                }
            }
            catch (Exception)
            {
           
            }

            return result;
        }

        public static void ReadDF(DiskDrive drive, string volumeName = "/storage/emulated")
        {
            string dfData = ExecuteProcess(new[] { "df", "-h" });

            if (string.IsNullOrEmpty(dfData) || drive == null)
                return;

            string[] lines = dfData.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);


            if (lines.Any(l => l.StartsWith(volumeName) || l.EndsWith(volumeName)))
            {
                string line = lines.Where(l => l.StartsWith(volumeName) || l.EndsWith(volumeName)).FirstOrDefault();

                string[] lineValues = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                // lineValues[0] := Dateisysstem (/dev/sda1)
                // lineValues[1] := Größe (4,0T)
                // lineValues[2] := Benutzt (3,3T)
                // lineValues[3] := Verfügbar (449G)
                // linesValue[4] := Verw% (82%)
                // linesValue[5] := Eingehängt auf (/media/server/Server)

                drive.TotalSpace = GeneralHelper.ParseDFEntry(lineValues[1]);
                drive.FreeSpace = GeneralHelper.ParseDFEntry(lineValues[3]);
            }
        }

        public static string ReadCPUName()
        {
            string result = ExecuteProcess(new[] { "/system/bin/cat", "/proc/cpuinfo" });

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
            string result = ExecuteProcess(new[] { "/system/bin/cat", "/proc/meminfo" }); 

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
                device.Environment.TotalRAM = ParseMemoryEntryInGB(memTotal);

                double totalGB = device.Environment.TotalRAM;
                double usedGB = totalGB - freeRam;
                int percentage = (int)System.Math.Round((usedGB / totalGB) * 100);

                device.Environment.FreeRAM = $"{System.Math.Round(usedGB, 2)} GB used ({percentage} %)";
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
                string unitValue = entries[1].Trim().ToLower();
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

        /// <summary>
        /// https://stackoverflow.com/a/42327441/6237448
        /// </summary>
        /// <returns></returns>
        public static Battery GetBatteryPercentage(Context context)
        {
            try
            {
                BatteryManager bm = (BatteryManager)context.GetSystemService(Context.BatteryService);

                Battery battery = new Battery();
                battery.IsCharging = bm.IsCharging;

                var status = (A.OS.BatteryHealth)bm.GetIntProperty((int)A.OS.BatteryProperty.Status);            
                battery.BatteryLevelInPercent =  bm.GetIntProperty((int)A.OS.BatteryProperty.Capacity);

                if (battery.BatteryLevelInPercent == int.MinValue || battery.BatteryLevelInPercent == int.MaxValue || status == BatteryHealth.Unknown)
                    return null;

                return battery;
            }
            catch
            {
                return null; // probably no battery
            }            
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
            currentDevice.ServiceClientVersion = $"vAndroid {Home.Data.Consts.HomeServiceAndroidClientVersion}";
#if NOGL
            currentDevice.ServiceClientVersion += " - NOGL";
#endif
            currentDevice.Environment.OSName = $"Android {Build.VERSION.Release}";
            currentDevice.Environment.OSVersion = $"{currentDevice.Environment.OSName} (Sec. Patch: {Build.VERSION.SecurityPatch}) ({System.Environment.OSVersion})";
            currentDevice.OS = Device.OSType.Android;
            currentDevice.Environment.CPUCount = DeviceInfoHelper.GetNumberOfCores();
            currentDevice.Environment.CPUName = DeviceInfoHelper.ReadCPUName();
            currentDevice.Environment.Description = Build.Model;
            currentDevice.Environment.DomainName = System.Environment.UserDomainName;
            currentDevice.Environment.Is64BitOS = System.Environment.Is64BitOperatingSystem;
            currentDevice.Environment.Product = Build.Product;
            currentDevice.Environment.StartTimestamp = dateTimeStarted;

            // Read and assign battery info
            currentDevice.BatteryInfo = GetBatteryPercentage(context);

            // Read and assign memory info
            DeviceInfoHelper.ReadAndAssignMemoryInfo(currentDevice);

            currentDevice.Environment.Vendor = Build.Brand;
            currentDevice.Environment.UserName = System.Environment.UserName;
            currentDevice.Environment.Motherboard = Build.Board;
            currentDevice.Environment.MachineName =
            currentDevice.Name = DeviceInfoHelper.GetDeviceName(cr);
            currentDevice.IP = DeviceInfoHelper.GetIpAddress(context);
            currentDevice.Environment.RunningTime = DateTime.Now.Subtract(dateTimeStarted);

            var dd = new DiskDrive() { VolumeName = "/", DriveName = "/", DriveID = "android_default_storage", PhysicalName = "android_default_storage" };
            ReadDF(dd);
            currentDevice.DiskDrives = new System.Collections.ObjectModel.ObservableCollection<DiskDrive>() { dd };
        }
    }
}