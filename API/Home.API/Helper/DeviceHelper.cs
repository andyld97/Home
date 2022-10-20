using Home.API.Controllers;
using Home.API.home;
using Home.API.home.Models;
using Home.Data;
using Home.Data.Com;
using Home.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
    public static class DeviceHelper
    {
        public static home.Models.Device UpdateDevice(HomeContext homeContext, home.Models.Device dbDevice, Device device, DeviceStatus status, DateTime now)
        {
            // Log: must not be added from device to dbDevice, because device itself won't log
            // Usage will be added to dbDevice in AckController so no need to add usage here

            dbDevice.Guid = device.ID;
            dbDevice.DeviceGroup = device.DeviceGroup;
            dbDevice.IsLive = device.IsLive;
            dbDevice.DeviceTypeId = (int)device.Type;
            dbDevice.Name = device.Name;
            dbDevice.Ostype = (int)device.OS;
            dbDevice.ServiceClientVersion = device.ServiceClientVersion;
            dbDevice.Location = device.Location;
            dbDevice.IsScreenshotRequired = device.IsScreenshotRequired;
            dbDevice.Ip = device.IP;
            dbDevice.Status = (status == DeviceStatus.Active);  // nullable, when Inactive?
            dbDevice.LastSeen = device.LastSeen;

            if (device.LastSeen == DateTime.MinValue)
                dbDevice.LastSeen = (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue;

            if (dbDevice.Environment == null)
                dbDevice.Environment = new home.Models.DeviceEnvironment();

            dbDevice.Environment.Cpucount = (short)device.Environment.CPUCount;
            dbDevice.Environment.Cpuname = device.Environment.CPUName;
            dbDevice.Environment.Cpuusage = device.Environment.CPUUsage;
            dbDevice.Environment.Description = device.Environment.Description;
            dbDevice.Environment.DiskUsage = device.Environment.DiskUsage;
            dbDevice.Environment.DomainName = device.Environment.DomainName;
            dbDevice.Environment.MachineName = device.Environment.MachineName;
            dbDevice.Environment.FreeRam = device.Environment.FreeRAM;
            dbDevice.Environment.TotalRam = device.Environment.TotalRAM;
            dbDevice.Environment.Is64BitOs = device.Environment.Is64BitOS;
            dbDevice.Environment.Motherboard = device.Environment.Motherboard;
            dbDevice.Environment.Osname = device.Environment.OSName;
            dbDevice.Environment.Osversion = device.Environment.OSVersion;
            dbDevice.Environment.Product = device.Environment.Product;
            dbDevice.Environment.RunningTime = device.Environment.RunningTime.Ticks;
            dbDevice.Environment.StartTimestamp = device.Environment.StartTimestamp;
            dbDevice.Environment.UserName = device.Environment.UserName;
            dbDevice.Environment.Vendor = device.Environment.Vendor;

            // ToDo: *** Battery shouldn't be an own table
            // dbDevice.Environment.Battery
            foreach (var disk in device.DiskDrives)
            {
                if (dbDevice.DeviceDiskDrive.Any(p => p.Guid == disk.UniqueID))
                {
                    // Just update disk
                    var diskToUpdate = dbDevice.DeviceDiskDrive.FirstOrDefault(d => d.Guid == disk.UniqueID);
                    ConvertDisk(diskToUpdate, disk);
                }
                else
                {
                    // Add disk
                    var dbDisk = new DeviceDiskDrive();
                    dbDisk.Device = dbDevice;
                    dbDevice.DeviceDiskDrive.Add(ConvertDisk(dbDisk, disk));
                }
            }

            foreach (var screenshot in device.ScreenshotFileNames)
            {
                if (DateTime.TryParseExact(screenshot, Consts.SCREENSHOT_DATE_FILE_FORMAT, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out DateTime dt))
                {
                    dbDevice.DeviceScreenshot.Add(new DeviceScreenshot()
                    {
                        Device = dbDevice,
                        ScreenshotFileName = screenshot,
                        Timestamp = dt
                    });
                }
            }

            foreach (var graphic in dbDevice.DeviceGraphic)
            {
                // Remove all cards which do not belong to this device anymore
                if (device.Environment.Graphics != graphic.Name && !device.Environment.GraphicCards.Contains(graphic.Name))
                      homeContext.DeviceGraphic.Remove(graphic);
            }

            foreach (var graphic in device.Environment.GraphicCards.Append(device.Environment.Graphics))
            {
                if (graphic == null)
                    continue;

                if (!dbDevice.DeviceGraphic.Any(x => x.Name == graphic))
                    dbDevice.DeviceGraphic.Add(new DeviceGraphic() { Device = dbDevice, Name = graphic });
            }

            return dbDevice;
        }

        public static home.Models.Device ConvertDevice(this HomeContext context, Device device)
        {
            home.Models.Device dbDevice = new home.Models.Device();
            UpdateDevice(context, dbDevice, device, DeviceStatus.Active, DateTime.Now);
            return dbDevice;
        }

        public static async Task<home.Models.Device> GetDeviceByIdAsync(this HomeContext context, string guid)
        {
            return await context.Device.Include(d => d.DeviceLog)
                                       .Include(p => p.DeviceGraphic)
                                       .Include(p => p.Environment)
                                       .Include(p => p.DeviceDiskDrive)
                                       .Include(p => p.DeviceType)
                                       .Include(p => p.DeviceScreenshot)
                                       .Include(p => p.DeviceUsage)
                                       .Include(p => p.DeviceCommand)
                                       .Include(p => p.DeviceMessage)
                                       .Include(p => p.OstypeNavigation).Where(p => p.Guid == guid).FirstOrDefaultAsync();
        }

        public static async Task<List<home.Models.Device>> GetAllDevicesAsync(this HomeContext context)
        {
            return await context.Device.Include(d => d.DeviceLog)
                                       .Include(p => p.DeviceGraphic)
                                       .Include(p => p.Environment)
                                       .Include(p => p.DeviceDiskDrive)
                                       .Include(p => p.DeviceType)
                                       .Include(p => p.DeviceScreenshot)
                                       .Include(p => p.DeviceUsage)
                                       .Include(p => p.DeviceCommand)
                                       .Include(p => p.DeviceMessage)
                                       .Include(p => p.OstypeNavigation).ToListAsync();
        }

        public static async Task<IEnumerable<home.Models.Device>> GetInactiveDevicesAsync(this HomeContext context)
        {
            var list = await context.Device.Include(d => d.DeviceLog)
                                           .Include(p => p.DeviceGraphic)
                                           .Include(p => p.Environment)
                                           .Include(p => p.DeviceDiskDrive)
                                           .Include(p => p.DeviceType)
                                           .Include(p => p.DeviceScreenshot)
                                           .Include(p => p.DeviceUsage)
                                           .Include(p => p.DeviceCommand)
                                           .Include(p => p.DeviceMessage)
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
            // result.Commands ToDo: ***
            result.BatteryInfo = null; // ToDo: ***
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

            // Hard disks
            foreach (var disk in device.DeviceDiskDrive)
            {
                result.DiskDrives.Add(new DiskDrive()
                {
                    DiskInterface = disk.DiskInterface,
                    DiskModel = disk.DiskModel,
                    DiskName = disk.DiskName,
                    DriveCompressed = disk.DriveCompressed,
                    DriveID = disk.DiskName,
                    DriveMediaType = (uint)disk.DriveMediaType,
                    DriveName = disk.DiskName,
                    DriveType = (uint)disk.DriveType,
                    FileSystem = disk.FileSystem,
                    FreeSpace = (ulong)disk.FreeSpace,
                    MediaSignature = (uint)disk.MediaSignature,
                    MediaType = disk.MediaType,
                    PhysicalName = disk.PhysicalName,
                    TotalSpace = (ulong)disk.TotalSpace,
                    VolumeName = disk.VolumeName,
                    VolumeSerial = disk.VolumeSerial,
                    MediaLoaded = disk.MediaLoaded.Value,
                    MediaStatus = disk.MediaStatus,
                });
            }

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

            return result;
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

        public static DeviceLog CreateLogEntry(home.Models.Device device, string message, LogEntry.LogLevel level, bool notifyTelegram = false)
        {
            var now = DateTime.Now;

            if (notifyTelegram && Program.GlobalConfig.UseWebHook)
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
    }
}
