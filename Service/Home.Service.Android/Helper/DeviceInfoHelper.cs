﻿using Android.Content;
using Android.Net;
using Android.OS;
using Home.Data.Helper;
using Home.Model;
using Java.Lang;
using static Android.Provider.Settings;
using Exception = Java.Lang.Exception;
using A = Android;
using Units;

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
            string dfData = ExecuteProcess(["df", "-h"]);

            if (string.IsNullOrEmpty(dfData) || drive == null)
                return;

            string[] lines = dfData.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);


            if (lines.Any(l => l.StartsWith(volumeName) || l.EndsWith(volumeName)))
            {
                string line = lines.Where(l => l.StartsWith(volumeName) || l.EndsWith(volumeName)).FirstOrDefault();

                string[] lineValues = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                // lineValues[0] := Dateisystem (/dev/sda1)
                // lineValues[1] := Größe (4,0T)
                // lineValues[2] := Benutzt (3,3T)
                // lineValues[3] := Verfügbar (449G)
                // linesValue[4] := Verw% (82%)
                // linesValue[5] := Eingehängt auf (/media/server/Server)

                drive.TotalSpace = GeneralHelper.ParseDFEntry(lineValues[1]);
                drive.FreeSpace = GeneralHelper.ParseDFEntry(lineValues[3]);
            }
            else
            {
                var s = new StatFs("/data");
                drive.TotalSpace = (ulong)(s.BlockCountLong * s.BlockSizeLong);
                drive.FreeSpace = (ulong)(s.FreeBlocksLong * s.BlockSizeLong);
            }
        }

        public static string ReadCPUName()
        {
            string result = ExecuteProcess(["/system/bin/cat", "/proc/cpuinfo"]);

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
            string result = ExecuteProcess(["/system/bin/cat", "/proc/meminfo"]);

            if (string.IsNullOrEmpty(result))
                return;

            string[] entries = result.Split(System.Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            if (entries.Length >= 2)
            {
                // MemTotal:       21530632 kB
                // MemFree:          351580 kB
                // MemAvailable:    1121215 kB
                // ...
                string memTotal = entries[0].ToLower().Replace("memtotal:", string.Empty).Trim();
                string memAvail = entries[2].ToLower().Replace("memavailable:", string.Empty).Trim();

                device.Environment.TotalRAM = GeneralHelper.ParseMemoryEntryInGB(memTotal);
                device.Environment.AvailableRAM = GeneralHelper.ParseMemoryEntryInGB(memAvail);
            }
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
                battery.BatteryLevelInPercent = bm.GetIntProperty((int)A.OS.BatteryProperty.Capacity);

                if (battery.BatteryLevelInPercent == int.MinValue || battery.BatteryLevelInPercent == int.MaxValue || status == BatteryHealth.Unknown)
                    return null;

                return battery;
            }
            catch
            {
                return null; // probably no battery
            }            
        }

        /// <summary>
        /// https://stackoverflow.com/questions/7071281/get-android-device-name/66651458#66651458 (modified)
        /// </summary>
        /// <param name="cr"></param>
        /// <returns></returns>
        public static string GetDeviceName(ContentResolver cr)
        {
            try
            {
                string userDeviceName = Global.GetString(cr, "device_name");
                if (userDeviceName == null)
                    userDeviceName = Secure.GetString(cr, "bluetooth_name");

                return userDeviceName;
            }
            catch
            {
                return System.Environment.MachineName; // probably localhost
            }
        }

        public static int GetNumberOfCores()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr1)
                return Runtime.GetRuntime().AvailableProcessors();

            return System.Environment.ProcessorCount;
        }

        public static string GetMacAddress()
        {
            if (A.OS.Build.VERSION.SdkInt < BuildVersionCodes.R)
            {
                try
                {
                    System.Collections.IList all = Java.Util.Collections.List(Java.Net.NetworkInterface.NetworkInterfaces);
                    foreach (Java.Net.NetworkInterface nif in all)
                    {
                        if (nif.Name != "wlan0") continue;

                        byte[] macBytes = nif.GetHardwareAddress();
                        if (macBytes == null)
                            return string.Empty;

                        var res1 = new StringBuilder();
                        foreach (byte b in macBytes)
                            res1.Append(Integer.ToHexString(b & 0xFF) + ":");

                        if (res1.Length() > 0)
                            res1.DeleteCharAt(res1.Length() - 1);
                        string result = res1.ToString();
                        if (!string.IsNullOrEmpty(result))
                            return result.ToUpper();
                    }
                }
                catch (Java.Lang.Exception)
                {
                }
            }

            // Seems not work >= Android 11
            // https://developer.android.com/training/articles/user-data-ids#mac-11-plus
            // But it's not that important to get the mac address on Android, because WOL is not really necessary for those devices

            return "02:00:00:00:00:00";
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
                return ipV4?.Replace("/24", string.Empty);

            if (ipAdresses.Count > 0)
                return ipAdresses.FirstOrDefault()?.Replace("/24", string.Empty);

            return string.Empty;
        }

        public static void RefreshDevice(this Device currentDevice, ContentResolver cr, Context context)
        {
            currentDevice.ServiceClientVersion = $"vAndroid{Home.Data.Consts.HomeServiceAndroidClientVersion}";
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
            currentDevice.MacAddress = DeviceInfoHelper.GetMacAddress();
            currentDevice.Environment.RunningTime = DateTime.Now.Subtract(dateTimeStarted);

            var dd = new DiskDrive() { VolumeName = "/", DriveName = "/", DriveID = "android_default_storage", PhysicalName = "android_default_storage" };
            ReadDF(dd);

            dd.MediaType = $"{currentDevice.Name} {currentDevice.ID}"; // ensure that disks can be added which are having the same amount of space (same GUID) (name is not enough, since you can have device with duplicate names!)
            currentDevice.DiskDrives = new System.Collections.ObjectModel.ObservableCollection<DiskDrive>() { dd };
        }
    }
}