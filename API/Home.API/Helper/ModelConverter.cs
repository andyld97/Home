using Home.API.Controllers;
using Home.API.home;
using Home.API.home.Models;
using Home.Data;
using Home.Data.Com;
using Home.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
            updateDevice.IsLive = device.IsLive;
            updateDevice.DeviceTypeId = (int)device.Type;
            updateDevice.Name = device.Name;
            updateDevice.Ostype = (int)device.OS;
            updateDevice.ServiceClientVersion = device.ServiceClientVersion;
            updateDevice.Location = device.Location;
            updateDevice.IsScreenshotRequired = device.IsScreenshotRequired;
            updateDevice.Ip = device.IP;
            updateDevice.Status = (status == DeviceStatus.Active);  // nullable, when Inactive?
            updateDevice.LastSeen = device.LastSeen;

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
            updateDevice.Environment.FreeRam = device.Environment.FreeRAM;
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
                    // ToDo: *** Also notify webhook (using device log)
                    logger.LogWarning($"Removed disk {disk.DiskName} from Device {updateDevice.Name} ...");
                    // updateDevice.DeviceDiskDrive.Remove(disk);
                    homeContext.DeviceDiskDrive.Remove(disk);
                }
            }

            foreach (var screenshot in device.ScreenshotFileNames)
            {
                if (DateTime.TryParseExact(screenshot, Consts.SCREENSHOT_DATE_FILE_FORMAT, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out DateTime dt))
                {
                    updateDevice.DeviceScreenshot.Add(new DeviceScreenshot()
                    {
                        Device = updateDevice,
                        ScreenshotFileName = screenshot,
                        Timestamp = dt
                    });
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

            return updateDevice;
        }

        public static home.Models.Device ConvertDevice(this HomeContext context, ILogger logger, Device device)
        {
            home.Models.Device dbDevice = new home.Models.Device();
            UpdateDevice(logger, context, dbDevice, device, DeviceStatus.Active, DateTime.Now);
            return dbDevice;
        }

        public static async Task<home.Models.Device> GetDeviceByIdAsync(this HomeContext context, string guid)
        {
            return await context.Device.Include(d => d.DeviceLog)
                                       .Include(p => p.DeviceGraphic)
                                       .Include(p => p.Environment)
                                       .ThenInclude(p => p.Battery)
                                       .Include(p => p.DeviceDiskDrive)
                                       .Include(p => p.DeviceType)
                                       .Include(p => p.DeviceScreenshot)
                                       .Include(p => p.DeviceUsage)
                                       .Include(p => p.DeviceCommand)
                                       .Include(p => p.DeviceMessage)
                                       .Include(p => p.DeviceWarning)
                                       .Include(p => p.OstypeNavigation).Where(p => p.Guid == guid).FirstOrDefaultAsync();
        }

        public static async Task<List<home.Models.Device>> GetAllDevicesAsync(this HomeContext context, bool noTracking)
        {
            var devices = context.Device.Include(d => d.DeviceLog)
                                       .Include(p => p.DeviceGraphic)
                                       .Include(p => p.Environment)
                                       .ThenInclude(p => p.Battery)
                                       .Include(p => p.DeviceDiskDrive)
                                       .Include(p => p.DeviceType)
                                       .Include(p => p.DeviceScreenshot)
                                       .Include(p => p.DeviceUsage)
                                       .Include(p => p.DeviceCommand)
                                       .Include(p => p.DeviceMessage)
                                       .Include(p => p.DeviceWarning)
                                       .Include(p => p.OstypeNavigation);

            if (noTracking)
                return await devices.AsNoTracking().ToListAsync();

            return await devices.ToListAsync();
        }

        public static async Task<IEnumerable<home.Models.Device>> GetInactiveDevicesAsync(this HomeContext context)
        {
            var list = await context.Device.Include(d => d.DeviceLog)
                                           .Include(p => p.DeviceGraphic)
                                           .Include(p => p.Environment)
                                           .ThenInclude(p => p.Battery)
                                           .Include(p => p.DeviceDiskDrive)
                                           .Include(p => p.DeviceType)
                                           .Include(p => p.DeviceScreenshot)
                                           .Include(p => p.DeviceUsage)
                                           .Include(p => p.DeviceCommand)
                                           .Include(p => p.DeviceMessage)
                                           .Include(p => p.DeviceWarning)
                                           .Include(p => p.OstypeNavigation).Where(d => d.Status).ToListAsync();

            return list.Where(d => d.LastSeen.Add(Program.GlobalConfig.RemoveInactiveClients) < DateTime.Now);
        }

        public static Device ConvertDevice(home.Models.Device device)
        {
            Device result = new Device();

            result.ServiceClientVersion = device.ServiceClientVersion;
            result.DeviceGroup = device.DeviceGroup;
            result.IP = device.Ip;
            result.IsLive = device.IsLive;
            result.IsScreenshotRequired = device.IsScreenshotRequired;
            result.LastSeen = device.LastSeen;
            result.Location = device.Location;
            result.Status = (device.Status ? DeviceStatus.Active : DeviceStatus.Offline);
            if (device.Environment.Battery != null)
                result.BatteryInfo = new Battery() { IsCharging = device.Environment.Battery.IsCharging, BatteryLevelInPercent = (int)device.Environment.Battery.Percentage };
            result.OS = (OSType)device.OstypeNavigation.OstypeId;
            result.Type = (Device.DeviceType)device.DeviceType.TypeId;
            result.ID = device.Guid;
            result.Name = device.Name;

            // Enviornment
            result.Environment = new Home.Model.DeviceEnvironment()
            {
                CPUCount = (int)device.Environment.Cpucount,
                CPUName = device.Environment.Cpuname,
                MachineName = device.Environment.MachineName,
                CPUUsage = device.Environment.Cpuusage.Value,
                Description = device.Environment.Description,
                DiskUsage = device.Environment.DiskUsage.Value,
                DomainName = device.Environment.DomainName,
                FreeRAM = device.Environment.FreeRam,
                Is64BitOS = device.Environment.Is64BitOs,
                Motherboard = device.Environment.Motherboard,
                OSName = device.Environment.Osname,
                OSVersion = device.Environment.Osversion,
                Product = device.Environment.Product,
                RunningTime = TimeSpan.FromTicks(device.Environment.RunningTime.Value),
                StartTimestamp = device.Environment.StartTimestamp.Value,
                TotalRAM = device.Environment.TotalRam.Value,
                UserName = device.Environment.UserName,
                Vendor = device.Environment.Vendor,
            };

            // Screenshot
            foreach (var item in device.DeviceScreenshot)
                result.ScreenshotFileNames.Add(item.ScreenshotFileName);

            // Log
            foreach (var item in device.DeviceLog)
                result.LogEntries.Add(new LogEntry(item.Timestamp.Value, item.Blob, (LogEntry.LogLevel)item.LogLevel, false));

            // Graphics cards
            foreach (var item in device.DeviceGraphic)
                result.Environment.GraphicCards.Add(item.Name);

            // Hard disks or SSDs
            foreach (var disk in device.DeviceDiskDrive)
                result.DiskDrives.Add(ConvertDisk(disk));

            // Usage
            result.Usage = new Home.Model.DeviceUsage();
            if (device.DeviceUsage != null)
            {
                foreach (var item in device.DeviceUsage.Cpu.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (double.TryParse(item, out double res))
                        result.Usage.AddCPUEntry(res);
                }

                foreach (var item in device.DeviceUsage.Ram.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (double.TryParse(item, out double res))
                        result.Usage.AddRAMEntry(res);
                }

                foreach (var item in device.DeviceUsage.Disk.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (double.TryParse(item, out double res))
                        result.Usage.AddDISKEntry(res);
                }

                if (!string.IsNullOrEmpty(device.DeviceUsage.Battery)) // battery is currently not implemented!
                {
                    foreach (var item in device.DeviceUsage.Battery.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
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
                else  if (warning.WarningType == (int)WarningType.StorageWarning)
                    result.StorageWarnings.Add( ModelConverter.ConvertStorageWarning(warning));
            }

            return result;
        }

        public static DiskDrive ConvertDisk(home.Models.DeviceDiskDrive dbDisk)
        {
            return new DiskDrive()
            {
                DiskInterface = dbDisk.DiskInterface,
                DiskModel = dbDisk.DiskModel,
                DiskName = dbDisk.DiskName,
                DriveCompressed = dbDisk.DriveCompressed,
                DriveID = dbDisk.DiskName,
                DriveMediaType = (uint)dbDisk.DriveMediaType,
                DriveName = dbDisk.DiskName,
                DriveType = (uint)dbDisk.DriveType,
                FileSystem = dbDisk.FileSystem,
                FreeSpace = (ulong)dbDisk.FreeSpace,
                MediaSignature = (uint)dbDisk.MediaSignature,
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
            dbDisk.MediaSignature = (int)diskDrive.MediaSignature;
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

        public static Command ConvertCommand(home.Models.DeviceCommand deviceCommand)
        {
            return new Command()
            {
                DeviceID = deviceCommand.Device.Guid,
                Executable = deviceCommand.Executable,
                Parameter = deviceCommand.Paramter,
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
                WarningOccoured = deviceWarning.Timestamp,
            };
        }

        public static StorageWarning ConvertStorageWarning(home.Models.DeviceWarning deviceWarning)
        {
            var storageWarning = new StorageWarning();
            storageWarning.WarningOccoured = deviceWarning.Timestamp;
            storageWarning.Value = (ulong)deviceWarning.CriticalValue;

            if (!string.IsNullOrEmpty(deviceWarning.AdditionalInfo) && deviceWarning.AdditionalInfo.Contains("|"))
            {
                string[] entries = deviceWarning.AdditionalInfo.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                if (entries.Length >= 2)
                {
                    storageWarning.StorageID = entries[0];
                    storageWarning.DiskName = entries[1];

                }
            }

            return storageWarning;
        }

        public static DeviceLog CreateLogEntry(home.Models.Device device, string message, LogEntry.LogLevel level, bool notifyWebHook = false)
        {
            var now = DateTime.Now;

            if (notifyWebHook && Program.GlobalConfig.UseWebHook)
            {
                string webHookMessage = $"[{now.ToShortDateString()} @ {now.ToShortTimeString()}]: {message}";
                Program.WebHookLogging.Enqueue(webHookMessage);
            }

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
                Program.WebHookLogging.Enqueue(logEntry.Message);

            return new DeviceLog()
            {
                Device = device,
                Blob = logEntry.Message,
                Timestamp = logEntry.Timestamp,
                LogLevel = (int)logEntry.Level
            };
        }
    }
}
