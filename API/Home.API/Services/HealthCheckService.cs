using Home.API.Helper;
using Home.API.home;
using Home.API.home.Models;
using Home.API.Model;
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
using System.Threading;
using System.Threading.Tasks;

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

                // Check devices
                foreach (var device in await homeContext.GetInactiveDevicesAsync())
                {
                    device.Status = false; // Device.DeviceStatus.Offline;

                    // If a device turns offline, usually the user wants to end the live state if the device is shutdown for example
                    device.IsLive = false;
                    bool notifyWebHook = (device.DeviceType.TypeId != (int)Home.Model.Device.DeviceType.Smartphone) &&  (device.DeviceType.TypeId == (int)Home.Model.Device.DeviceType.SingleBoardDevice || device.DeviceType.TypeId == (int)Home.Model.Device.DeviceType.Server);
                    var logEntry = DeviceHelper.CreateLogEntry(device, $"No activity detected ... Device \"{device.Name}\" was flagged as offline!", LogEntry.LogLevel.Information, notifyWebHook);
                    await homeContext.DeviceLog.AddAsync(logEntry);

                    NotifyClientQueues(EventQueueItem.EventKind.DeviceChangedState, DeviceHelper.ConvertDevice(device));
                }

                // Aquiring a new screenshot for all online devices (except android devices)
                foreach (var device in await homeContext.GetAllDevicesAsync())
                {
                    if (device.IsScreenshotRequired)
                        continue;

                    if (device.DeviceScreenshot.Count == 0)
                        continue;

                    var shot = device.DeviceScreenshot.LastOrDefault();
                    if (shot == null)
                        continue;

                    // Check age of this screenshot
                    if (shot.Timestamp.Add(Program.GlobalConfig.AquireNewScreenshot) < DateTime.Now)
                    {
                        device.IsScreenshotRequired = true;
                        var logEntry = DeviceHelper.CreateLogEntry(device, $"Last screenshot was older than {Program.GlobalConfig.AquireNewScreenshot.TotalHours}h. Aquiring a new screenshot ...", LogEntry.LogLevel.Information);
                        await homeContext.DeviceLog.AddRangeAsync(logEntry);
                        NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, DeviceHelper.ConvertDevice(device));
                    }
                }

                // Delete screenshots which are older than one day
                foreach (var device in await DeviceHelper.GetAllDevicesAsync(homeContext))
                {
                    if (device.DeviceScreenshot.Count == 0 || device.DeviceScreenshot.Count == 1)
                        continue;

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
                        device.DeviceScreenshot.Remove(shot);
                        string path = System.IO.Path.Combine(Config.SCREENSHOTS_PATH, device.Guid, $"{shot}.png");
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

                // Check for obsolete warnings
                // ToDo: *** Warnings
                /*foreach (var device in Devices)
                {
                    // Battery Warnings
                    if (device.BatteryWarning != null && device.BatteryWarning.CanBeRemoved(device, GlobalConfig.BatteryWarningPercentage))
                    {
                        device.BatteryWarning = null;
                        device.LogEntries.Add(new LogEntry("[Battery Warning]: Removed!", LogEntry.LogLevel.Information, true));
                    }

                    // Storage Warnings
                    if (device.StorageWarnings.Count == 0)
                        continue;

                    List<StorageWarning> toRemove = new List<StorageWarning>();
                    foreach (var warning in device.StorageWarnings)
                    {
                        var associatedDisk = device.DiskDrives.FirstOrDefault(d => d.UniqueID == warning.StorageID);
                        if (associatedDisk == null)
                            continue;

                        if (warning.CanBeRemoved(associatedDisk, GlobalConfig.StorageWarningPercentage))
                        {
                            // Add log entry
                            toRemove.Add(warning);
                            device.LogEntries.Add(new LogEntry($"[Storage Warning]: Removed for DISK \"{associatedDisk}\"", LogEntry.LogLevel.Information, true));
                            NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, device);
                        }
                    }

                    foreach (var warning in toRemove)
                        device.StorageWarnings.Remove(warning);
                }*/


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

                // Save devices (ToDo: *** Only save if there are any changes recieved from the controller!)
                /*try
                {
                    lock (Devices)
                    {
                        foreach (var device in Devices)
                        {
                            if (device.LogEntries.Count >= 200)
                            {
                                while (device.LogEntries.Count != 100 - 2)
                                    device.LogEntries.RemoveAt(0);

                                device.LogEntries.Insert(0, new LogEntry("Truncated log file of this device!", LogEntry.LogLevel.Information));
                                NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, device);
                            }
                        }

                        Serialization.Serialization.Save<List<Device>>(Config.DEVICE_PATH, Devices, Serialization.Serialization.Mode.Normal);
                    }
                }
                catch
                { }*/


                await homeContext.SaveChangesAsync();
            }
        }


        /// <summary>
        /// Notifies all active client queues that there is a new event for the device
        /// </summary>
        /// <param name="eventKind"></param>
        /// <param name="device"></param>
        private static void NotifyClientQueues(EventQueueItem.EventKind eventKind, Home.Model.Device device)
        {
            lock (Program.EventQueues)
            {
                foreach (var queue in Program.EventQueues)
                {
                    var now = DateTime.Now;
                    queue.LastEvent = now;
                    queue.Events.Enqueue(new EventQueueItem() { DeviceID = device.ID, EventDescription = eventKind, EventOccured = now, EventData = new EventData(device) });
                }
            }
        }
    }
}
