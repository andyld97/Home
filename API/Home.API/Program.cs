using Home.Data;
using Home.Data.Events;
using Home.Data.Helper;
using Home.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Timers;

namespace Home.API
{
    public class Program
    {
        private static ILogger _logger;
        public readonly static List<EventQueue> EventQueues = new List<EventQueue>();
        public readonly static List<Client> Clients = new List<Client>();
        public readonly static Dictionary<Client, List<string>> LiveModeAssoc = new Dictionary<Client, List<string>>();
        public static List<Device> Devices = new List<Device>();

        private static readonly Timer healthCheckTimer = new Timer();
        private static bool isHealthCheckTimerActive = false;
        private static readonly object _lock = new object();

        public static readonly string DEVICE_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devices.xml");
        public static readonly string SCREENSHOTS_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");

        public static void Main(string[] args)
        { 
            // Create a logger to enable logging also here in Program.cs (see https://stackoverflow.com/a/62404676/6237448)
            var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
            _logger = loggerFactory.CreateLogger<Startup>();

            // Load devices
            if (System.IO.File.Exists(DEVICE_PATH))
            {
                try
                {
                    Devices = Serialization.Serialization.Read<List<Device>>(DEVICE_PATH, Serialization.Serialization.Mode.Normal);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to read devices.xml: {ex.Message}");
                }

                if (Devices == null)
                    Devices = new List<Device>();
            }
                    
            // Initalize health check timer
            healthCheckTimer.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds;
            healthCheckTimer.Elapsed += HealthCheckTimer_Elapsed;
            healthCheckTimer.Start();

            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Notifies all active client queues that there is a new event for the device
        /// </summary>
        /// <param name="eventKind"></param>
        /// <param name="device"></param>
        private static void NotifyClientQueues(EventQueueItem.EventKind eventKind, Device device)
        {
            lock (EventQueues)
            {
                foreach (var queue in EventQueues)
                {
                    var now = DateTime.Now;
                    queue.LastEvent = now;
                    queue.Events.Enqueue(new EventQueueItem() { DeviceID = device.ID, EventDescription = eventKind, EventOccured = now, EventData = new EventData(device) });
                }
            }
        }

        private static async void HealthCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                if (isHealthCheckTimerActive)
                    return;
                else
                    isHealthCheckTimerActive = false;
            }

            // Check event queues
            List<string> clientIDsToRemove = new List<string>();
            lock (EventQueues)
            {
                var deadEventQueues = EventQueues.Where(e => e.LastClientRequest.AddMinutes(10) < DateTime.Now).ToList();
                foreach (var deq in deadEventQueues)
                {
                    EventQueues.Remove(deq);
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
            lock (Devices)
            {
                foreach (var device in Devices.Where(p => p.Status != Device.DeviceStatus.Offline && p.LastSeen.AddMinutes(3) < DateTime.Now))
                {
                    device.Status = Device.DeviceStatus.Offline;

                    // If a device turns offline, usually the user wants to end the live state if the device is shutdown for example
                    device.IsLive = false;
                    device.LogEntries.Add(new LogEntry(DateTime.Now, $"No activity detected ... Device \"{device.Name}\" was flagged as offline!", LogEntry.LogLevel.Warning, (device.Type == Device.DeviceType.SingleBoardDevice || device.Type == Device.DeviceType.Server)));

                    NotifyClientQueues(EventQueueItem.EventKind.DeviceChangedState, device);
                }

                // Aquiring a new screenshot for all online devices (except android devices)
                foreach (var device in Devices.Where(p => p.Status != Device.DeviceStatus.Offline && p.OS != Device.OSType.Android))
                {
                    if (device.IsScreenshotRequired)
                        continue;

                    if (device.ScreenshotFileNames.Count == 0)
                        continue;

                    string screenshotFileName = device.ScreenshotFileNames.LastOrDefault();
                    if (string.IsNullOrEmpty(screenshotFileName))
                        continue;

                    // Check age of this screenshot
                    if (DateTime.TryParseExact(screenshotFileName, Consts.SCREENSHOT_DATE_FILE_FORMAT, System.Globalization.CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime result) && result.AddHours(12) < DateTime.Now)
                    {
                        device.IsScreenshotRequired = true;
                        device.LogEntries.Add(new LogEntry(DateTime.Now, "Last screenshot was older than 12h. Aquiring a new screenshot ...", LogEntry.LogLevel.Information));
                        NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, device);
                    }
                }

                // Delete screenshots which are older than one day
                foreach (var device in Devices)
                {
                    if (device.ScreenshotFileNames.Count == 0 || device.ScreenshotFileNames.Count == 1)
                        continue;

                    List<string> screenshotsToRemove = new List<string>();
                    foreach (var shot in device.ScreenshotFileNames.Take(device.ScreenshotFileNames.Count - 1))
                    {
                        if (DateTime.TryParseExact(shot, Consts.SCREENSHOT_DATE_FILE_FORMAT, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime result) && result.AddDays(1) < DateTime.Now)
                        {
                            screenshotsToRemove.Add(shot);
                            _logger.LogInformation($"Deleted screenshot {shot} from device {device.Name}, because it is older than one day!");
                        }
                    }

                    foreach (var shot in screenshotsToRemove)
                    {
                        device.ScreenshotFileNames.Remove(shot);
                        string path = System.IO.Path.Combine(SCREENSHOTS_PATH, device.ID, $"{shot}.png");
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
                foreach (var device in Devices)
                {
                    // Battery Warnings
                    if (device.BatteryWarning != null && device.BatteryWarning.CanBeRemoved(device))
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

                        if (warning.CanBeRemoved(associatedDisk))
                        {
                            // Add log entry
                            toRemove.Add(warning);
                            device.LogEntries.Add(new LogEntry($"[Storage Warning]: Removed for DISK \"{associatedDisk}\"", LogEntry.LogLevel.Information, true));
                            NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, device);
                        }
                    }

                    foreach (var warning in toRemove)
                        device.StorageWarnings.Remove(warning);
                }
            }

            // Check for tg logging (extract log messages)
            List<LogEntry> logEntries = new List<LogEntry>();
            lock (Devices)
            {
                foreach (var device in Devices)
                {
                    foreach (var logEntry in device.LogEntries.Where(l => l.LogTelegram))
                    {
                        // Add to list and reset telegram flag
                        logEntries.Add(logEntry);
                        logEntry.LogTelegram = false;
                    }

                }
            }

            // Send log messages
            foreach (var log in logEntries)
                await TGLogger.LogTelegram(log.ToString());

            // Save devices (TODO: *** Only save if there are any changes recieved from the controller!)
            try
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

                    Serialization.Serialization.Save<List<Device>>(DEVICE_PATH, Devices, Serialization.Serialization.Mode.Normal);
                }
            }
            catch
            { }

            lock (_lock)
            {
                isHealthCheckTimerActive = false;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:5250");
                });
    }
}
