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
            lock (EventQueues)
            {
                var deadEventQueues = EventQueues.Where(e => e.LastClientRequest.AddHours(1) < DateTime.Now).ToList();
                foreach (var deq in deadEventQueues)
                {
                    EventQueues.Remove(deq);
                    _logger.LogInformation($"Event Queue {deq} was removed due to lost activity ...");
                }
            }

            // Check devices
            lock (Devices)
            {
                foreach (var device in Devices.Where(p => p.Status != Device.DeviceStatus.Offline && p.LastSeen.AddHours(1) < DateTime.Now))
                    device.Status = Device.DeviceStatus.Offline;
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
