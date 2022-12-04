using Home.API.Helper;
using Home.API.home;
using Home.API.home.Models;
using Home.Data;
using Home.Data.Events;
using Home.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Device = Home.API.home.Models.Device;

namespace Home.API.Services
{
    public class HealthCheckService : BackgroundService
    {
        private readonly ILogger<HealthCheckService> _logger;
        private IServiceScopeFactory serviceProvider;

        private int hour = -1;

        public HealthCheckService(ILogger<HealthCheckService> logger, IServiceScopeFactory serviceProvider)
        {
            _logger = logger;
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Currently each hour will resets this, so in the worst case you will get a notication every hour (still better than each time the service runs)
        /// ACK Errors gets LOGGED only ONCE per DEVICE. So if there is a an ack error you have to handle it,
        /// but mostly such an error don't occur for only one device, but rather for all devices (e.g. if the db connection is lost)
        /// </summary>
        /// <returns>true if the webhook should be notified</returns>
        private bool ShouldNotifyWebHook()
        {
            bool shouldLog = false;
            int h = DateTime.Now.Hour;
            if (hour == -1)
            {
                shouldLog = true;
                hour = h;
            }
            else if (hour != h)
            {
                hour = h;
                shouldLog = true;
            }

            return shouldLog;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {           
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay((int)Program.GlobalConfig.HealthCheckTimerInterval.TotalMilliseconds);

                DateTime now = DateTime.Now;
                var scope =  serviceProvider.CreateAsyncScope();
                var homeContext = scope.ServiceProvider.GetService<HomeContext>();

                // Check event queues
                List<string> clientIDsToRemove = new List<string>();
                lock (Program.EventQueues)
                {
                    var deadEventQueues = Program.EventQueues.Where(e => e.LastClientRequest.Add(Program.GlobalConfig.RemoveInactiveGUIClients) < DateTime.Now).ToList();
                    foreach (var deq in deadEventQueues)
                    {
                        Program.EventQueues.Remove(deq);
                        clientIDsToRemove.Add(deq.ClientID);
                        _logger.LogInformation($"Event Queue {deq} was removed due to lost activity ...");
                    }
                }

                // If an event queue is removed, the associated client should also be removed!
                if (clientIDsToRemove.Count > 0)
                {
                    lock (Program.Clients)
                    {
                        foreach (var id in clientIDsToRemove)
                        {
                            var client = Program.Clients.Where(p => p.ID == id).FirstOrDefault();
                            if (client != null)
                            {
                                _logger.LogInformation($"Client {client.ID} was removed due to lost activity ...");
                                Program.Clients.Remove(client);
                            }
                        }
                    }
                }

                // This is all be done in one loop to prevent multiple db calls
                try
                {
                    // [INFO]: We have to include all tables here to ensure that the event queues can be notified (device conversation)
                    var devices = await homeContext.GetAllDevicesAsync(false);

                    ParallelOptions parallelOptions = new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        CancellationToken = stoppingToken,
                    };

                    await Parallel.ForEachAsync(devices, parallelOptions, async (device, token) =>
                    {
                        // Update the device status if it is inactive
                        await UpdateDeviceStatusAsync(homeContext, device);

                        // Aquiring a new screenshot for all online devices (except android devices)
                        await CheckForScreenshotExpiredAsync(homeContext, device, now);

                        // Delete screenshots which are older than the Program.GlobalConfig.RemoveOldScreenshots-Timestamp
                        await CleanUpScreenshotsAsync(homeContext, device, now);

                        // Check if there are any warnings to remove
                        await UpdateDeviceWarningsAsync(homeContext, device);

                        // Ensure that the device log doesn't blow up
                        await TruncateDeviceLogAsync(homeContext, device);
                    });
                }
                catch (Exception ex)
                {
                    if (ex is not TaskCanceledException)
                    {
                        _logger.LogError($"Critical Exception from HealthCheck-Service: {ex.ToString()}");

                        if (ShouldNotifyWebHook())
                            await WebHook.NotifyWebHookAsync(Program.GlobalConfig.WebHookUrl, $"CRICTIAL EXCEPTION from Background Service: {ex.ToString()}");
                    }
                }
 
                if (Program.GlobalConfig.UseWebHook)
                {
                    // Send log messages
                    while (!Program.WebHookLogging.IsEmpty)
                    {
                        if (Program.WebHookLogging.TryDequeue(out string log))
                            await WebHook.NotifyWebHookAsync(Program.GlobalConfig.WebHookUrl, log);
                        else
                            break;
                    }
                }

                try
                {
                    await homeContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    if (ex is not TaskCanceledException)
                    {
                        _logger.LogError($"Critical Exception from HealthCheck-Service (while saving): {ex.Message}");

                        if (ShouldNotifyWebHook())
                            await WebHook.NotifyWebHookAsync(Program.GlobalConfig.WebHookUrl, $"CRICTIAL EXCEPTION from Background Service: {ex.ToString()}");
                    }
                }
            }
        }

        #region Health Service Tasks

        private async Task UpdateDeviceStatusAsync(HomeContext homeContext, Device device)
        {
            if (device.Status && device.LastSeen.Add(Program.GlobalConfig.RemoveInactiveClients) < DateTime.Now)
            {
                device.Status = false; // Device.DeviceStatus.Offline;

                // If a device turns offline, usually the user wants to end the live state if the device is shutdown for example
                device.IsLive = false;
                bool notifyWebHook = (device.DeviceTypeId != (int)Home.Model.Device.DeviceType.Smartphone) && (device.DeviceTypeId == (int)Home.Model.Device.DeviceType.SingleBoardDevice || device.DeviceTypeId == (int)Home.Model.Device.DeviceType.Server);
                var logEntry = ModelConverter.CreateLogEntry(device, $"No activity detected ... Device \"{device.Name}\" was flagged as offline!", LogEntry.LogLevel.Information, notifyWebHook);
                await homeContext.DeviceLog.AddAsync(logEntry);

                ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.DeviceChangedState, device);
            }
        }

        private async Task CheckForScreenshotExpiredAsync(HomeContext homeContext, Device device, DateTime now)
        {
            // Only check for devices which are online
            if (!device.Status)
                return;

            if (device.IsScreenshotRequired)
                return;

            if (device.DeviceScreenshot.Count == 0)
                return;

            // Check for the last screenshot's age
            var shot = device.DeviceScreenshot.LastOrDefault();
            if (shot == null)
                return;

            // Check age of this screenshot
            if (shot.Timestamp.Add(Program.GlobalConfig.AquireNewScreenshot) < now)
            {
                device.IsScreenshotRequired = true;
                var logEntry = ModelConverter.CreateLogEntry(device, $"Last screenshot was older than {Program.GlobalConfig.AquireNewScreenshot.TotalHours}h. Aquiring a new screenshot ...", LogEntry.LogLevel.Information);
                await homeContext.DeviceLog.AddRangeAsync(logEntry);
                ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, device);
            }
        }

        private async Task CleanUpScreenshotsAsync(HomeContext homeContext, Device device, DateTime now)
        {
            if (device.DeviceScreenshot.Count == 0 || device.DeviceScreenshot.Count == 1)
                return;           

            List<DeviceScreenshot> screenshotsToRemove = new List<DeviceScreenshot>();

            // Consider multiple screens screenshot handling!!!
            // One screenshot must be remained either for a general screenshot or per each screen         
            var nonAssociatedScreenshots = device.DeviceScreenshot.Where(p => p.ScreenId == null).ToList();
            var assoicatedScreenshots = device.DeviceScreenshot.Where(p => p.ScreenId != null).GroupBy(p => p.ScreenId);

            // 1. Add all "general" screenshots except one (if there is only one, just leave it)
            if (nonAssociatedScreenshots.Count > 1)
            {
                foreach (var shot in nonAssociatedScreenshots.Take(nonAssociatedScreenshots.Count - 1))
                {
                    if (shot.Timestamp.Add(Program.GlobalConfig.RemoveOldScreenshots) < now)
                    {
                        screenshotsToRemove.Add(shot);
                        _logger.LogInformation($"Deleted screenshot {shot} from device {device.Name}, because it is older than {Program.GlobalConfig.RemoveOldScreenshots}!");
                    }
                }
            }

            // 2. Add all screenshots per screen except one (if there is only one, just leave it)
            if (assoicatedScreenshots.Count() > 1)
            {
                foreach (var shot in assoicatedScreenshots) // group
                {
                    var shots = shot.ToList();
                    foreach (var item in shots.Take(shots.Count - 1))
                    {
                        if (item.Timestamp.Add(Program.GlobalConfig.RemoveOldScreenshots) < now)
                        {
                            screenshotsToRemove.Add(item);
                            _logger.LogInformation($"Deleted screenshot {item} from device {device.Name}, because it is older than {Program.GlobalConfig.RemoveOldScreenshots}!");
                        }
                    }
                }
            }

            foreach (var shot in screenshotsToRemove)
            {
                shot.Device = null;
                homeContext.DeviceScreenshot.Remove(shot);

                string path = System.IO.Path.Combine(Config.SCREENSHOTS_PATH, device.Guid, $"{shot.ScreenshotFileName}.png");
                try
                {
                    System.IO.File.Delete(path);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to remove screenshot {ex.Message}");
                }
            }
        }

        private async Task UpdateDeviceWarningsAsync(HomeContext homeContext, Device device)
        {
            // Battery Warnings
            var batteryWarning = device.DeviceWarning.Where(w => w.WarningType == (int)WarningType.BatteryWarning).FirstOrDefault();

            // If Battery Warning is not empty but the device has no battery, also remove the warning
            if ((batteryWarning != null && device.Environment.Battery == null) || (batteryWarning != null && GeneralHelper.ConvertNullableValue(device.Environment.Battery.Percentage, out int per) && ModelConverter.ConvertBatteryWarning(batteryWarning).CanBeRemoved(per, Program.GlobalConfig.BatteryWarningPercentage)))
            {
                batteryWarning.Device = null;
                homeContext.DeviceWarning.Remove(batteryWarning);
                var logEntry = ModelConverter.CreateLogEntry(device, $"[Battery Warning]: Removed for device {device.Name}!", LogEntry.LogLevel.Information, true);
                await homeContext.DeviceLog.AddAsync(logEntry);
            }

            // Storage Warnings
            var storageWarnings = device.DeviceWarning.Where(w => w.WarningType == (int)WarningType.StorageWarning).Select(p => (ModelConverter.ConvertStorageWarning(p), p)).ToList();
            if (storageWarnings.Count > 0)
            {
                List<DeviceWarning> toRemove = new List<DeviceWarning>();
                foreach (var warning in storageWarnings)
                {
                    var associatedDisk = device.DeviceDiskDrive.FirstOrDefault(d => d.Guid == warning.Item1.StorageID);
                    if (associatedDisk == null)
                        continue;

                    bool remove = false;
                    try
                    {
                        if (warning.Item1.CanBeRemoved(ModelConverter.ConvertDisk(associatedDisk), Program.GlobalConfig.StorageWarningPercentage))
                            remove = true;
                    }
                    catch (ArgumentException)
                    {
                        // CHECK IF THE DISK FOR THE STORAGE WARNING STILL EXISTS, OTHERWISE REMOVE THE WARNING
                        // So, ArgumentException will be thrown if the disk is not existent anymore, so the warning can be removed
                        remove = true;
                    }

                    if (remove)
                    {
                        // Add log entry
                        toRemove.Add(warning.p);                                               
                        var logEntry = ModelConverter.CreateLogEntry(device, $"[Storage Warning]: Removed for DISK \"{associatedDisk.DriveName}\"", LogEntry.LogLevel.Information, true);
                        await homeContext.DeviceLog.AddAsync(logEntry);
                        ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, device);
                    }
                }

                foreach (var warning in toRemove)
                {
                    warning.Device = null;
                    homeContext.DeviceWarning.Remove(warning);
                }
            }
        }

        private async Task TruncateDeviceLogAsync(HomeContext homeContext, Device device)
        {
            if (device.DeviceLog.Count >= 200)
            {
                var entries = device.DeviceLog.OrderBy(p => p.Timestamp).ToList();

                while (device.DeviceLog.Count != 100 - 2)
                {
                    var entry = entries.FirstOrDefault();
                    entry.Device = null;
                    device.DeviceLog.Remove(entry);
                    entries.RemoveAt(0);
                }

                var logEntry = ModelConverter.CreateLogEntry(device, "Truncated log file of this device!", LogEntry.LogLevel.Information);
                await homeContext.DeviceLog.AddAsync(logEntry);

                ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, device);
            }
        }

        #endregion
    }
}
