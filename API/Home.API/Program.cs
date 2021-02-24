using Home.Data;
using Home.Data.Events;
using Home.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using static Home.Data.Helper.GeneralHelper;

namespace Home.API
{
    public class Program
    {
        private static ILogger _logger;
        public readonly static List<EventQueue> EventQueues = new List<EventQueue>();
        public readonly static List<Client> Clients = new List<Client>();
        public static List<Device> Devices = new List<Device>();

        private static readonly Timer healthCheckTimer = new Timer();
        private static bool isHealthCheckTimerActive = false;
        private static readonly object _lock = new object();

        public static readonly string Device_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devices.xml");
        public static readonly string SCREENSHOTS_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");

        public static void Main(string[] args)
        { 
            // Create a logger to enable logging also here in Program.cs (see https://stackoverflow.com/a/62404676/6237448)
            var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
            _logger = loggerFactory.CreateLogger<Startup>();

            // Load devices
            if (System.IO.File.Exists(Device_PATH))
            {
                try
                {
                    Devices = Serialization.Serialization.Read<List<Device>>(Device_PATH, Serialization.Serialization.Mode.Normal);
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

        private static void HealthCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
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
                foreach (var device in Devices.Where(p => p.Status != Device.DeviceStatus.Offline && p.LastSeen.AddMinutes(10) < DateTime.Now))
                {
                    lock (EventQueues)
                    {
                        device.Status = Device.DeviceStatus.Offline;
                        device.LogEntries.Add("No activity detected ... Device was flagged as offline!".FormatLogLine(DateTime.Now));

                        foreach (var queue in EventQueues)
                        {
                            var now = DateTime.Now;
                            queue.LastEvent = now;
                            queue.Events.Enqueue(new EventQueueItem() { DeviceID = device.ID, EventDescription = EventQueueItem.EventKind.DeviceChangedState, EventOccured = now, EventData = new EventData(device) });
                        }
                    }
                    
                }
            }

            // Save devices (TODO: Only save if there are any changes recieved from the controller!)
            try
            {
                lock (Devices)
                {
                    Serialization.Serialization.Save<List<Device>>(Device_PATH, Devices, Serialization.Serialization.Mode.Normal);
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
