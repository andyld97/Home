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

        public HealthCheckService(ILogger<HealthCheckService> logger, IServiceScopeFactory serviceProvider)
        {
            _logger = logger;
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {           
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay((int)Program.GlobalConfig.HealthCheckTimerInterval.TotalMilliseconds);

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
                    foreach (var device in await homeContext.GetAllDevicesAsync(false))
                    {
                        // Update the device status if it is inactive
                        await UpdateDeviceStatusAsync(homeContext, device);

                        // Aquiring a new screenshot for all online devices (except android devices)
                        await CheckForScreenshotsAsync(homeContext, device);

                        // Delete screenshots which are older than one day
                        await CleanUpScreenshotsAsync(homeContext, device);

                        // Check if there are any warnings to remove
                        await UpdateDeviceWarningsAsync(homeContext, device);

                        // Ensure that the device log doesn't blow up
                        await TruncateDeviceLogAsync(homeContext, device);
                    }
                }
                catch (Exception ex)
                {
                    await WebHook.NotifyWebHookAsync(Program.GlobalConfig.WebHookUrl, $"CRICTIAL EXCEPTION from Background Service: {ex.ToString()}");
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
                    await WebHook.NotifyWebHookAsync(Program.GlobalConfig.WebHookUrl, $"CRICTIAL EXCEPTION from Background Service: {ex.ToString()}");
                    return;
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
                bool notifyWebHook = (device.DeviceType.TypeId != (int)Home.Model.Device.DeviceType.Smartphone) && (device.DeviceType.TypeId == (int)Home.Model.Device.DeviceType.SingleBoardDevice || device.DeviceType.TypeId == (int)Home.Model.Device.DeviceType.Server);
                var logEntry = ModelConverter.CreateLogEntry(device, $"No activity detected ... Device \"{device.Name}\" was flagged as offline!", LogEntry.LogLevel.Information, notifyWebHook);
                await homeContext.DeviceLog.AddAsync(logEntry);

                ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.DeviceChangedState, ModelConverter.ConvertDevice(device));
            }
        }

        private async Task CheckForScreenshotsAsync(HomeContext homeContext, Device device)
        {
            if (device.IsScreenshotRequired)
                return;

            if (device.DeviceScreenshot.Count == 0)
                return;

            var shot = device.DeviceScreenshot.LastOrDefault();
            if (shot == null)
                return;

            // Check age of this screenshot
            if (shot.Timestamp.Add(Program.GlobalConfig.AquireNewScreenshot) < DateTime.Now)
            {
                device.IsScreenshotRequired = true;
                var logEntry = ModelConverter.CreateLogEntry(device, $"Last screenshot was older than {Program.GlobalConfig.AquireNewScreenshot.TotalHours}h. Aquiring a new screenshot ...", LogEntry.LogLevel.Information);
                await homeContext.DeviceLog.AddRangeAsync(logEntry);
                ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, ModelConverter.ConvertDevice(device));
            }
        }

        private async Task CleanUpScreenshotsAsync(HomeContext homeContext, Device device)
        {
            if (device.DeviceScreenshot.Count == 0 || device.DeviceScreenshot.Count == 1)
                return;

            List<DeviceScreenshot> screenshotsToRemove = new List<DeviceScreenshot>();
            foreach (var shot in device.DeviceScreenshot.Take(device.DeviceScreenshot.Count - 1))
            {
                if (shot.Timestamp.Add(Program.GlobalConfig.RemoveOldScreenshots) < DateTime.Now)
                {
                    screenshotsToRemove.Add(shot);
                    _logger.LogInformation($"Deleted screenshot {shot} from device {device.Name}, because it is older than one day!");
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
            if (batteryWarning != null && ModelConverter.ConvertBatteryWarning(batteryWarning).CanBeRemoved(ModelConverter.ConvertDevice(device), Program.GlobalConfig.BatteryWarningPercentage))
            {
                batteryWarning.Device = null;
                homeContext.DeviceWarning.Remove(batteryWarning);
                var logEntry = ModelConverter.CreateLogEntry(device, "[Battery Warning]: Removed!", LogEntry.LogLevel.Information, true);
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

                    if (warning.Item1.CanBeRemoved(ModelConverter.ConvertDisk(associatedDisk), Program.GlobalConfig.StorageWarningPercentage))
                    {
                        // Add log entry
                        toRemove.Add(warning.p);
                        var logEntry = ModelConverter.CreateLogEntry(device, $"[Storage Warning]: Removed for DISK \"{associatedDisk}\"", LogEntry.LogLevel.Information, true);
                        await homeContext.DeviceLog.AddAsync(logEntry);
                        ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, ModelConverter.ConvertDevice(device));
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

                ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, ModelConverter.ConvertDevice(device));
            }
        }

        #endregion
    }
}
