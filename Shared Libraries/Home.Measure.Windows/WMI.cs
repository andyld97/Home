﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows.Forms;

namespace Home.Measure.Windows
{
    /// <summary>
    /// Capsels all methods available via WMI
    /// </summary>
    public static class WMI
    {
        private static string ConvertUint16ToString(this UInt16[] value)
        {
            var arr = (value.Where(p => p != 0).Select(p => (char)p)).ToArray();
            string result = string.Empty;
            for (int i = 0; i < arr.Length; i++)
                result += arr[i];

            return result;
        }

        private static JArray GetScreenInfoFromWMI()
        {
            JArray result = new JArray();
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorID"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        JObject info = new JObject();
                        int manWeek = 0, manYear = 0;
                        foreach (PropertyData property in mo.Properties)
                        {
                            if (property.Name == "InstanceName")
                                info["id"] = property.Value.ToString();
                            else if (property.Name == "ManufacturerName" && property.Value is UInt16[] value)
                                info["manufacturer"] = value.ConvertUint16ToString();
                            else if (property.Name == "SerialNumberID" && property.Value is UInt16[] val)
                                info["serial"] = val.ConvertUint16ToString();
                            else if (property.Name == "WeekOfManufacture")
                            {
                                if (property.Value != null && int.TryParse(property.Value.ToString(), out manWeek))
                                { }
                            }
                            else if (property.Name == "YearOfManufacture")
                            {
                                if (property.Value != null && int.TryParse(property.Value.ToString(), out manYear))
                                { }
                            }
                        }

                        info["built_date"] = $"{manWeek}/{manYear}";
                        result.Add(info);
                    }
                }
            }
            catch
            {

            }

            return result;
        }

        public static JArray GetScreenInformation()
        {
            var wmiInfo = GetScreenInfoFromWMI();
            JArray screens = new JArray();
            int index = 0;

            foreach (var item in System.Windows.Forms.Screen.AllScreens.OrderBy(a => a.DeviceName))
            {
                JObject screen = new JObject();

                if (wmiInfo.Count > index)
                {
                    screen["id"] = wmiInfo[index]["id"];
                    screen["manufacturer"] = wmiInfo[index]["manufacturer"];
                    screen["serial"] = wmiInfo[index]["serial"];
                    screen["built_date"] = wmiInfo[index]["built_date"];
                }
                else
                {
                    screen["id"] = item.DeviceName;
                    screen["manufacturer"] = "Unknown";
                    screen["serial"] = "Unknown";
                    screen["built_date"] = "Unknown";
                }

                screen["index"] = index++;
                screen["is_primary"] = item.Primary;

                string screenName = string.Empty;

                try
                {
                    screenName = item.DeviceFriendlyName();
                }
                catch
                {
                    
                }

                if (string.IsNullOrEmpty(screenName?.Trim()))
                    screenName = item.DeviceName;

                screen["device_name"] = screenName;
                screen["resolution"] = $"{item.Bounds.Width}x{item.Bounds.Height}";
                
                screens.Add(screen);
            }

            return screens;
        }

        public static List<string> DetermineGraphicsCardNames()
        {
            // Alternative: SELECT VideoProcessor FROM Win32_VideoController
            List<string> devices = new List<string>();
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DisplayConfiguration"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        foreach (PropertyData property in mo.Properties)
                        {
                            if (property.Name == "Description")
                                devices.Add(property.Value.ToString().Trim());
                        }
                    }
                }
            }
            catch
            {

            }

            try
            {
                using (ManagementObjectSearcher searcher2 = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DisplayControllerConfiguration"))
                {
                    foreach (ManagementObject queryObj in searcher2.Get())
                    {
                        string name = queryObj["Name"].ToString();
                        if (name == "Current Display Controller Configuration")
                            continue;

                        // Prevent (example: above NVIDIA GeForce GTX650, this GeForce GTX650)
                        if (devices.Any(p => p.Contains(name)))
                            continue;

                        devices.Add(name.Trim());
                    }
                }
            }
            catch
            {

            }

            // ToDo!
            /*using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    Debug.WriteLine("Name  -  " + obj["Name"] + "</br>");
                    Debug.WriteLine("DeviceID  -  " + obj["DeviceID"] + "</br>");
                    Debug.WriteLine("AdapterRAM  -  " + obj["AdapterRAM"] + "</br>");
                    Debug.WriteLine("AdapterDACType  -  " + obj["AdapterDACType"] + "</br>");
                    Debug.WriteLine("Monochrome  -  " + obj["Monochrome"] + "</br>");
                    Debug.WriteLine("InstalledDisplayDrivers  -  " + obj["InstalledDisplayDrivers"] + "</br>");
                    Debug.WriteLine("DriverVersion  -  " + obj["DriverVersion"] + "</br>");
                    Debug.WriteLine("VideoProcessor  -  " + obj["VideoProcessor"] + "</br>");
                    Debug.WriteLine("VideoArchitecture  -  " + obj["VideoArchitecture"] + "</br>");
                    Debug.WriteLine("VideoMemoryType  -  " + obj["VideoMemoryType"] + "</br>");
                }
            }*/

            return devices.Distinct().OrderBy(s => s).ToList();
        }

        public static string DetermineCPUName()
        {
            try
            {
                using (ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor"))
                {
                    foreach (ManagementObject mo in mos.Get())
                    {
                        string value = mo["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(value))
                            return value.Trim();
                    }
                }
            }
            catch
            {
                // Log
            }

            return string.Empty;
        }

        public static string DetermineMotherboard()
        {
            try
            {
                using (ManagementObjectSearcher baseboardSearcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BaseBoard"))
                {
                    using (ManagementObjectSearcher motherboardSearcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_MotherboardDevice"))
                    {
                        foreach (ManagementObject queryObj in baseboardSearcher.Get())
                        {
                            string product = queryObj["Product"].ToString();
                            string vendor = queryObj["Manufacturer"].ToString();

                            return $"{vendor} {product}";
                        }
                    }
                }
            }
            catch
            {
                // Log
            }

            return string.Empty;
        }

        public static void GetBIOSInfo(out string vendor, out string version, out string description, out DateTime? releaseDate)
        {
            vendor = string.Empty;
            version = string.Empty;
            description = string.Empty;
            releaseDate = null;

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS"))
            {
                foreach (ManagementObject mo in searcher.Get())
                {
                    foreach (PropertyData property in mo.Properties)
                    {
                        if (property.Name == "Manufacturer")
                            vendor = property.Value?.ToString();
                        else if (property.Name == "SMBIOSBIOSVersion")
                            version = property.Value?.ToString();
                        else if (property.Name == "Description")
                            description = property.Value?.ToString();
                        else if (property.Name == "ReleaseDate" && property.Type == CimType.DateTime)
                        {
                            // See https://github.com/vinaypamnani/wmie2/blob/master/WmiExplorer/Classes/ManagementBaseObjectPropertyDescriptor.cs#L206 for reference
                            string dateString = property.Value.ToString();
                            try
                            {
                                releaseDate = new DateTime(
                                int.Parse(dateString.Substring(0, 4)),
                                int.Parse(dateString.Substring(4, 2)),
                                int.Parse(dateString.Substring(6, 2)),
                                int.Parse(dateString.Substring(8, 2)),
                                int.Parse(dateString.Substring(10, 2)),
                                int.Parse(dateString.Substring(12, 2)),
                                int.Parse(dateString.Substring(15, 6)) / 1000, DateTimeKind.Local);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                releaseDate = null;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// May not be compatible with Windows XP
        /// </summary>
        /// <param name="product"></param>
        /// <param name="description"></param>
        /// <param name="vendor"></param>
        public static void GetVendorInfo(out string product, out string description, out string vendor)
        {
            product = string.Empty;
            description = string.Empty;
            vendor = string.Empty;

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystemProduct"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        foreach (PropertyData property in mo.Properties)
                        {
                            if (property.Name == "Name")
                                product = property.Value?.ToString();
                            else if (property.Name == "Description")
                                description = property.Value?.ToString();
                            else if (property.Name == "Vendor")
                                vendor = property.Value?.ToString();
                        }
                    }
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// Returns information of all hard drives (as a serialized json string)
        /// </summary>
        /// <returns>Serialized disk drive list as json</returns>
        public static string DetermineDiskDrives()
        {
            List<JObject> result = new List<JObject>();

            try
            {
                using (var driveQuery = new ManagementObjectSearcher("select * from Win32_DiskDrive"))
                {
                    foreach (ManagementObject d in driveQuery.Get())
                    {
                        var deviceId = d.Properties["DeviceId"].Value;
                        var partitionQueryText = string.Format("associators of {{{0}}} where AssocClass = Win32_DiskDriveToDiskPartition", d.Path.RelativePath);
                        var partitionQuery = new ManagementObjectSearcher(partitionQueryText);

                        try
                        {
                            foreach (ManagementObject p in partitionQuery.Get())
                            {
                                var logicalDriveQueryText = string.Format("associators of {{{0}}} where AssocClass = Win32_LogicalDiskToPartition", p.Path.RelativePath);
                                var logicalDriveQuery = new ManagementObjectSearcher(logicalDriveQueryText);

                                foreach (ManagementObject ld in logicalDriveQuery.Get())
                                {
                                    try
                                    {
                                        var physicalName = Convert.ToString(d.Properties["Name"].Value); // \\.\PHYSICALDRIVE2
                                        var diskName = Convert.ToString(d.Properties["Caption"].Value); // WDC WD5001AALS-xxxxxx
                                        var diskModel = Convert.ToString(d.Properties["Model"].Value); // WDC WD5001AALS-xxxxxx
                                        var diskInterface = Convert.ToString(d.Properties["InterfaceType"].Value); // IDE
                                        var capabilities = (ushort[])d.Properties["Capabilities"].Value; // 3,4,7 - random access, supports writing, 7=removable device
                                        var mediaLoaded = Convert.ToBoolean(d.Properties["MediaLoaded"].Value); // bool
                                        var mediaType = Convert.ToString(d.Properties["MediaType"].Value); // Fixed hard disk media
                                        var mediaSignature = Convert.ToInt64(d.Properties["Signature"].Value); // int32
                                        var mediaStatus = Convert.ToString(d.Properties["Status"].Value); // OK

                                        var driveName = Convert.ToString(ld.Properties["Name"].Value); // C:
                                        var driveId = Convert.ToString(ld.Properties["DeviceId"].Value); // C:
                                        var pnpDriveId = Convert.ToString(d.Properties["PNPDeviceID"].Value);
                                        var driveCompressed = Convert.ToBoolean(ld.Properties["Compressed"].Value);
                                        var driveType = Convert.ToUInt32(ld.Properties["DriveType"].Value); // C: - 3
                                        var fileSystem = Convert.ToString(ld.Properties["FileSystem"].Value); // NTFS
                                        var freeSpace = Convert.ToUInt64(ld.Properties["FreeSpace"].Value); // in bytes
                                        var totalSpace = Convert.ToUInt64(ld.Properties["Size"].Value); // in bytes
                                        var driveMediaType = Convert.ToUInt32(ld.Properties["MediaType"].Value); // c: 12
                                        var volumeName = Convert.ToString(ld.Properties["VolumeName"].Value); // System
                                        var volumeSerial = Convert.ToString(ld.Properties["VolumeSerialNumber"].Value); // 12345678

                                        JObject dd = new JObject
                                        {
                                            ["physical_name"] = physicalName,
                                            ["disk_name"] = diskName,
                                            ["disk_model"] = diskModel,
                                            ["disk_interface"] = diskInterface,
                                            ["media_loaded"] = mediaLoaded,
                                            ["media_type"] = mediaType,
                                            ["media_signature"] = mediaSignature,
                                            ["media_status"] = mediaStatus,
                                            ["drive_name"] = driveName,
                                            ["drive_id"] = driveId,
                                            ["pnp_drive_id"] = pnpDriveId,
                                            ["drive_compressed"] = driveCompressed,
                                            ["drive_type"] = driveType,
                                            ["file_system"] = fileSystem,
                                            ["free_space"] = freeSpace,
                                            ["total_space"] = totalSpace,
                                            ["drive_media_type"] = driveMediaType,
                                            ["volume_name"] = volumeName,
                                            ["volume_serial"] = volumeSerial
                                        };

                                        result.Add(dd);
                                    }
                                    catch (Exception ex)
                                    {
                                        // ToDO: Log
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // ToDO: Log
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ToDO: Log
            }

            var finalResult = result.OrderBy(p => p["drive_id"]).ToList();
            return JsonConvert.SerializeObject(finalResult);
        }
    }
}