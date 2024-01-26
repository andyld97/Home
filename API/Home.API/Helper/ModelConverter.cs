using Home.API.Controllers;
using Home.API.home;
using Home.API.home.Models;
using Home.Data;
using Home.Data.Com;
using Home.Model;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Writers;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebhookAPI;
using static Home.Model.Device;
using Device = Home.Model.Device;

namespace Home.API.Helper
{
    public static class ModelConverter
    {
        public static home.Models.Device UpdateDevice(ILogger logger, HomeContext homeContext, home.Models.Device updateDevice, Device device, DeviceStatus status, DateTime now)
        {
            // Log: must not be added from device to dbDevice, because device itself won't log
            // Usage will be added to dbDevice in AckController so no need to add usage here

            updateDevice.Guid = device.ID;
            updateDevice.DeviceGroup = device.DeviceGroup;
            // updateDevice.IsLive = device.IsLive; // This isn't to be updated, because the ack device don't know about the live status
            updateDevice.DeviceTypeId = (int)device.Type;
            updateDevice.Name = device.Name;
            updateDevice.Ostype = (int)device.OS;
            updateDevice.ServiceClientVersion = device.ServiceClientVersion;
            updateDevice.Location = device.Location;
            updateDevice.Ip = device.IP;
            updateDevice.MacAddress = device.MacAddress;
            updateDevice.Status = (status == DeviceStatus.Active);  // nullable, when Inactive?
            updateDevice.LastSeen = device.LastSeen;
            // updateDevice.IsScreenshotRequired = device.IsScreenshotRequired; // This isn't to be updated, because the ack device don't know about the screenshot status

            if (device.LastSeen == DateTime.MinValue)
                updateDevice.LastSeen = (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue;

            if (updateDevice.Environment == null)
                updateDevice.Environment = new home.Models.DeviceEnvironment();

            updateDevice.Environment.Cpucount = (short)device.Environment.CPUCount;
            updateDevice.Environment.Cpuname = device.Environment.CPUName;
            updateDevice.Environment.Cpuusage = device.Environment.CPUUsage;
            updateDevice.Environment.Description = device.Environment.Description;
            updateDevice.Environment.DiskUsage = device.Environment.DiskUsage;
            updateDevice.Environment.DomainName = device.Environment.DomainName;
            updateDevice.Environment.MachineName = device.Environment.MachineName;
            updateDevice.Environment.AvailableRam = device.Environment.AvailableRAM;
            updateDevice.Environment.TotalRam = device.Environment.TotalRAM;
            updateDevice.Environment.Is64BitOs = device.Environment.Is64BitOS;
            updateDevice.Environment.Motherboard = device.Environment.Motherboard;
            updateDevice.Environment.Osname = device.Environment.OSName;
            updateDevice.Environment.Osversion = device.Environment.OSVersion;
            updateDevice.Environment.Product = device.Environment.Product;
            updateDevice.Environment.RunningTime = device.Environment.RunningTime.Ticks;
            updateDevice.Environment.StartTimestamp = device.Environment.StartTimestamp;
            updateDevice.Environment.UserName = device.Environment.UserName;
            updateDevice.Environment.Vendor = device.Environment.Vendor;

            if (device.BatteryInfo != null)
            {
                if (updateDevice.Environment.Battery == null)
                    updateDevice.Environment.Battery = new DeviceBattery();

                updateDevice.Environment.Battery.IsCharging = device.BatteryInfo.IsCharging;
                updateDevice.Environment.Battery.Percentage = device.BatteryInfo.BatteryLevelInPercent;
            }

            foreach (var disk in device.DiskDrives)
            {
                if (updateDevice.DeviceDiskDrive.Any(p => p.Guid == disk.UniqueID))
                {
                    // Just update disk
                    var diskToUpdate = updateDevice.DeviceDiskDrive.FirstOrDefault(d => d.Guid == disk.UniqueID);
                    ConvertDisk(diskToUpdate, disk);
                }
                else
                {
                    // Add disk
                    var dbDisk = new DeviceDiskDrive();
                    dbDisk.Device = updateDevice;
                    updateDevice.DeviceDiskDrive.Add(ConvertDisk(dbDisk, disk));
                }
            }

            // Remove all non (anymore) existent disks
            foreach (var disk in updateDevice.DeviceDiskDrive)
            {
                if (!device.DiskDrives.Any(d => d.UniqueID == disk.Guid))
                {
                    disk.Device = null;
                    // ToDo: *** Also notify webhook (using device log, if it's not a removable device)?
                    logger.LogWarning($"Removed disk {disk.DiskName} from Device {updateDevice.Name} ...");
                    homeContext.DeviceDiskDrive.Remove(disk);
                }
            }

            // Remove all cards which do not belong to this device anymore
            foreach (var graphic in updateDevice.DeviceGraphic)
            {
                if (device.Environment.Graphics != graphic.Name && !device.Environment.GraphicCards.Contains(graphic.Name))
                {
                    // ToDo: *** Also notify webhook (using device log)
                    logger.LogWarning($"Removed graphics card {graphic.Name} from Device {updateDevice.Name} ...");
                    homeContext.DeviceGraphic.Remove(graphic);
                }
            }

            foreach (var graphic in device.Environment.GraphicCards.Append(device.Environment.Graphics))
            {
                if (graphic == null)
                    continue;

                if (!updateDevice.DeviceGraphic.Any(x => x.Name == graphic))
                    updateDevice.DeviceGraphic.Add(new DeviceGraphic() { Device = updateDevice, Name = graphic });            
            }

            // Screen(s) can be empty but ofc not null
            foreach (var screen in device.Screens)
            {
                if (string.IsNullOrEmpty(screen.ID)) continue; // ID must be always set, otherwise we cannot continue

                var dbScreen = updateDevice.DeviceScreen.Where(s => s.ScreenId == screen.ID).FirstOrDefault();
                if (dbScreen == null)
                    updateDevice.DeviceScreen.Add(ConvertScreen(screen, updateDevice));
                else
                {
                    // Update
                    dbScreen.IsPrimary = screen.IsPrimary;
                    dbScreen.ScreenIndex = screen.Index;
                    dbScreen.DeviceName = screen.DeviceName;
                    dbScreen.Resolution = screen.Resolution;
                    dbScreen.ScreenId = screen.ID;
                    dbScreen.Manufacturer = screen.Manufacturer;
                    dbScreen.Serial = screen.Serial;
                    dbScreen.BuiltDate = screen.BuiltDate;
                }
            }

            // Check for screen(s) which not belong to the device anymore
            List<DeviceScreen> toRemove = new List<DeviceScreen>();
            foreach (var screen in updateDevice.DeviceScreen)
            {
                if (device.Screens.Any(sr => sr.ID == screen.ScreenId))
                    continue;

                // Delete it
                toRemove.Add(screen);   
            }

            foreach (var item in toRemove)
                homeContext.DeviceScreen.Remove(item);

            // Check BIOS
            if (device.BIOS != null)
            {
                var bios = updateDevice.DeviceBios.FirstOrDefault();
                DateTime? releaseDate = device.BIOS.ReleaseDate;
                if (releaseDate == DateTime.MinValue)
                    releaseDate = null; 

                if (bios == null)
                {
                    updateDevice.DeviceBios.Add(new DeviceBios() 
                    {
                        Description = device.BIOS.Description,
                        Vendor = device.BIOS.Vendor,
                        ReleaseDate = releaseDate,
                        Version = device.BIOS.Version,
                    });
                }
                else
                {
                    bios.ReleaseDate = releaseDate;
                    bios.Version = device.BIOS.Version;
                    bios.Vendor = device.BIOS.Vendor;   
                    bios.Description = device.BIOS.Description;
                }
            }

            return updateDevice;
        }

        public static home.Models.Device ConvertDevice(this HomeContext context, ILogger logger, Device device)
        {
            home.Models.Device dbDevice = new home.Models.Device();
            UpdateDevice(logger, context, dbDevice, device, DeviceStatus.Active, DateTime.Now);
            return dbDevice;
        }

        public static Device ConvertDevice(home.Models.Device device)
        {
            try
            {
                Device result = new Device();

                result.ServiceClientVersion = device.ServiceClientVersion;
                result.DeviceGroup = device.DeviceGroup;
                result.IP = device.Ip;
                result.MacAddress = device.MacAddress;
                result.IsLive = device.IsLive;
                result.IsScreenshotRequired = device.IsScreenshotRequired;
                result.LastSeen = device.LastSeen;
                result.Location = device.Location;
                result.Status = (device.Status ? DeviceStatus.Active : DeviceStatus.Offline);
                if (device.Environment.Battery != null)
                    result.BatteryInfo = new Battery() { IsCharging = device.Environment.Battery.IsCharging, BatteryLevelInPercent = (int)device.Environment.Battery.Percentage };
                result.OS = (OSType)device.OstypeNavigation.OstypeId;
                result.Type = (Device.DeviceType)device.DeviceTypeId;
                result.ID = device.Guid;
                result.Name = device.Name;

                // Environment
                result.Environment = new Home.Model.DeviceEnvironment()
                {
                    CPUCount = (int)device.Environment.Cpucount,
                    CPUName = device.Environment.Cpuname,
                    MachineName = device.Environment.MachineName,
                    CPUUsage = device.Environment.Cpuusage.Value,
                    Description = device.Environment.Description,
                    DiskUsage = device.Environment.DiskUsage.Value,
                    DomainName = device.Environment.DomainName,
                    AvailableRAM = device.Environment.AvailableRam ?? 0,
                    Is64BitOS = device.Environment.Is64BitOs,
                    Motherboard = device.Environment.Motherboard,
                    OSName = device.Environment.Osname,
                    OSVersion = device.Environment.Osversion,
                    Product = device.Environment.Product,
                    StartTimestamp = device.Environment.StartTimestamp.Value,
                    TotalRAM = device.Environment.TotalRam.Value,
                    UserName = device.Environment.UserName,
                    Vendor = device.Environment.Vendor,
                };
                if (device.Environment.RunningTime != null)
                    result.Environment.RunningTime = TimeSpan.FromTicks(device.Environment.RunningTime.Value);

                // Screenshot
                foreach (var item in device.DeviceScreenshot)
                    result.Screenshots.Add(new Screenshot() { ScreenIndex = item.Screen?.ScreenIndex, Filename = item.ScreenshotFileName, Timestamp = item.Timestamp });

                // Log
                foreach (var item in device.DeviceLog)
                    result.LogEntries.Add(new LogEntry(item.Timestamp.Value, item.Blob, (LogEntry.LogLevel)item.LogLevel, false));

                // Graphics cards
                foreach (var item in device.DeviceGraphic)
                    result.Environment.GraphicCards.Add(item.Name);

                // Hard disks or SSDs
                foreach (var disk in device.DeviceDiskDrive)
                    result.DiskDrives.Add(ConvertDisk(disk));

                // Screens
                foreach (var screen in device.DeviceScreen)
                    result.Screens.Add(ConvertScreen(screen));

                // Device changes
                foreach (var change in device.DeviceChange)
                    result.DevicesChanges.Add(new DeviceChangeEntry() { Timestamp = change.Timestamp, Description = change.Description, Type = (DeviceChangeEntry.DeviceChangeType)change.Type });

                // BIOS
                if (device.DeviceBios.FirstOrDefault() != null)
                {
                    var bios = device.DeviceBios.FirstOrDefault();
                    result.BIOS = new BIOS()
                    {
                        Description = bios.Description,
                        ReleaseDate = bios.ReleaseDate ?? DateTime.MinValue,
                        Vendor = bios.Vendor,
                        Version = bios.Version  
                    };
                }

                // Usage
                result.Usage = new Home.Model.DeviceUsage();
                if (device.DeviceUsage != null)
                {
                    if (!string.IsNullOrEmpty(device.DeviceUsage.Cpu))
                    {
                        foreach (var item in device.DeviceUsage.Cpu.Split(new string[] { Consts.StatsSeperator }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (double.TryParse(item, out double res))
                                result.Usage.AddCPUEntry(res);
                        }
                    }

                    if (!string.IsNullOrEmpty(device.DeviceUsage.Ram))
                    {
                        foreach (var item in device.DeviceUsage.Ram.Split(new string[] { Consts.StatsSeperator }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (double.TryParse(item, out double res))
                                result.Usage.AddRAMEntry(res);
                        }
                    }

                    if (!string.IsNullOrEmpty(device.DeviceUsage.Disk))
                    {
                        foreach (var item in device.DeviceUsage.Disk.Split(new string[] { Consts.StatsSeperator }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (double.TryParse(item, out double res))
                                result.Usage.AddDISKEntry(res);
                        }
                    }

                    if (!string.IsNullOrEmpty(device.DeviceUsage.Battery))
                    {
                        foreach (var item in device.DeviceUsage.Battery.Split(new string[] { Consts.StatsSeperator }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (int.TryParse(item, out int res))
                                result.Usage.AddBatteryEntry(res);
                        }
                    }
                }

                // Also add warnings (if any)
                foreach (var warning in device.DeviceWarning)
                {
                    if (warning.WarningType == (int)WarningType.BatteryWarning)
                        result.BatteryWarning = ModelConverter.ConvertBatteryWarning(warning);
                    else if (warning.WarningType == (int)WarningType.StorageWarning)
                        result.StorageWarnings.Add(ModelConverter.ConvertStorageWarning(warning));
                }

                return result;
            }
            catch (Exception ex)
            {
                Program.WebHookLogging.Enqueue((Webhook.LogLevel.Error, $"Failed to convert device: {ex.Message}", device.Name));
                throw;
            }
        }

        public static DiskDrive ConvertDisk(home.Models.DeviceDiskDrive dbDisk)
        {
            return new DiskDrive()
            {
                DiskInterface = dbDisk.DiskInterface,
                DiskModel = dbDisk.DiskModel,
                DiskName = dbDisk.DiskName,
                DriveCompressed = dbDisk.DriveCompressed,
                DriveID = dbDisk.DriveId,
                DriveMediaType = (uint)dbDisk.DriveMediaType,
                DriveName = dbDisk.DriveName,
                DriveType = (uint)dbDisk.DriveType,
                FileSystem = dbDisk.FileSystem,
                FreeSpace = (ulong)dbDisk.FreeSpace,
                MediaSignature = (ulong)dbDisk.MediaSignature,
                MediaType = dbDisk.MediaType,
                PhysicalName = dbDisk.PhysicalName,
                TotalSpace = (ulong)dbDisk.TotalSpace,
                VolumeName = dbDisk.VolumeName,
                VolumeSerial = dbDisk.VolumeSerial,
                MediaLoaded = dbDisk.MediaLoaded.Value,
                MediaStatus = dbDisk.MediaStatus,
            };
        }

        public static home.Models.DeviceDiskDrive ConvertDisk(home.Models.DeviceDiskDrive dbDisk, DiskDrive diskDrive)
        {
            dbDisk.DiskName = diskDrive.DiskName;
            dbDisk.DiskInterface = diskDrive.DiskInterface;
            dbDisk.DiskModel = diskDrive.DiskModel;
            dbDisk.DriveId = diskDrive.DriveID;
            dbDisk.DriveMediaType = (int)diskDrive.DriveMediaType;
            dbDisk.Guid = diskDrive.UniqueID;
            dbDisk.DriveType = (int)diskDrive.DriveType;
            dbDisk.DriveName = diskDrive.DriveName;
            dbDisk.FileSystem = diskDrive.FileSystem;
            dbDisk.MediaSignature = (long)diskDrive.MediaSignature;
            dbDisk.MediaType = diskDrive.MediaType;
            dbDisk.PhysicalName = diskDrive.PhysicalName;
            dbDisk.TotalSpace = (long)diskDrive.TotalSpace;
            dbDisk.VolumeName = diskDrive.VolumeName;
            dbDisk.VolumeSerial = diskDrive.VolumeSerial;
            dbDisk.DriveCompressed = diskDrive.DriveCompressed;
            dbDisk.FreeSpace = (long)diskDrive.FreeSpace;
            dbDisk.MediaStatus = diskDrive.MediaStatus;
            dbDisk.MediaLoaded = diskDrive.MediaLoaded;

            return dbDisk;
        }

        public static Screen ConvertScreen(this home.Models.DeviceScreen deviceScreen)
        {
            return new Screen()
            {
                DeviceName = deviceScreen.DeviceName,
                Index = deviceScreen.ScreenIndex,
                IsPrimary = deviceScreen.IsPrimary,
                Resolution = deviceScreen.Resolution,
                BuiltDate = deviceScreen.BuiltDate,
                ID = deviceScreen.ScreenId,
                Serial = deviceScreen.Serial,
                Manufacturer = deviceScreen.Manufacturer
            };
        }

        public static home.Models.DeviceScreen ConvertScreen(this Screen screen, home.Models.Device device)
        {
            return new home.Models.DeviceScreen
            {
                Device = device,
                DeviceName = screen.DeviceName,
                IsPrimary = screen.IsPrimary,
                Resolution= screen.Resolution,
                ScreenIndex = screen.Index,
                BuiltDate = screen.BuiltDate,
                ScreenId = screen.ID,
                Manufacturer= screen.Manufacturer,
                Serial = screen.Serial,
            };
        }

        public static Command ConvertCommand(home.Models.DeviceCommand deviceCommand)
        {
            return new Command()
            {
                DeviceID = deviceCommand.Device.Guid,
                Executable = deviceCommand.Executable,
                Parameter = deviceCommand.Parameter,
            };
        }

        public static Message ConvertMessage(home.Models.DeviceMessage message)
        {
            return new Message()
            {
                Content = message.Content,
                Title = message.Title,
                DeviceID = message.Device.Guid,
                Type = (Message.MessageImage)message.Type
            };
        }

        public static BatteryWarning ConvertBatteryWarning(home.Models.DeviceWarning deviceWarning)
        {
            return new BatteryWarning()
            {
                Value = (int)deviceWarning.CriticalValue,
                WarningOccurred = deviceWarning.Timestamp,
            };
        }

        public static StorageWarning ConvertStorageWarning(home.Models.DeviceWarning deviceWarning)
        {
            var storageWarning = new StorageWarning();
            storageWarning.WarningOccurred = deviceWarning.Timestamp;
            storageWarning.Value = (ulong)deviceWarning.CriticalValue;

            if (!string.IsNullOrEmpty(deviceWarning.AdditionalInfo) && deviceWarning.AdditionalInfo.Contains(Consts.StatsSeperator))
            {
                string[] entries = deviceWarning.AdditionalInfo.Split(new string[] { Consts.StatsSeperator }, StringSplitOptions.RemoveEmptyEntries);

                if (entries.Length >= 2)
                {
                    storageWarning.StorageID = entries[0];
                    storageWarning.DiskName = entries[1];
                }
                else if (entries.Length == 1)
                {
                    storageWarning.StorageID = entries[0];
                }
            }

            return storageWarning;
        }

        public static DeviceLog CreateLogEntry(home.Models.Device device, string message, LogEntry.LogLevel level, bool notifyWebHook = false)
        {
            var now = DateTime.Now;

            if (notifyWebHook && Program.GlobalConfig.UseWebHook)
                Program.WebHookLogging.Enqueue((ConvertLogLevelForWebhook(level), message, device.Name));

            return new DeviceLog()
            {
                Device = device,
                Blob = message,
                Timestamp = DateTime.Now,
                LogLevel = (int)level
            };
        }

        public static DeviceLog ConvertLogEntry(home.Models.Device device, LogEntry logEntry)
        {
            if (logEntry.NotifyWebHook && Program.GlobalConfig.UseWebHook)
                Program.WebHookLogging.Enqueue((ConvertLogLevelForWebhook(logEntry.Level), logEntry.Message, device.Name));

            return new DeviceLog()
            {
                Device = device,
                Blob = logEntry.Message,
                Timestamp = logEntry.Timestamp,
                LogLevel = (int)logEntry.Level
            };
        }

        private static Webhook.LogLevel ConvertLogLevelForWebhook(LogEntry.LogLevel level)
        {
            Webhook.LogLevel dLevel = Webhook.LogLevel.Info;
            switch (level)
            {
                case LogEntry.LogLevel.Debug:
                case LogEntry.LogLevel.Information: dLevel = Webhook.LogLevel.Info; break;
                case LogEntry.LogLevel.Warning: dLevel = Webhook.LogLevel.Warning; break;
                case LogEntry.LogLevel.Error: dLevel = Webhook.LogLevel.Error; break;
            }

            return dLevel;
        }
        public static string GetDescription<TEnum>(this TEnum enumValue) where TEnum : struct
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                return attribute.Description;

            return enumValue.ToString();
        }
    }
}