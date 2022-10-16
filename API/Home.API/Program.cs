using Home.API.Helper;
using Home.API.home;
using Home.API.Model;
using Home.Data;
using Home.Data.Events;
using Home.Data.Helper;
using Home.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using static Home.API.Helper.DeviceHelper;

namespace Home.API
{
    public class Program
    {
        private static ILogger _logger;
        public readonly static List<EventQueue> EventQueues = new List<EventQueue>();
        public readonly static List<Client> Clients = new List<Client>();
        public readonly static Dictionary<Client, List<string>> LiveModeAssoc = new Dictionary<Client, List<string>>();
        public readonly static Dictionary<Device, bool> AckErrorSentAssoc = new Dictionary<Device, bool>();
        public static ConcurrentQueue<string> WebHookLogging = new ConcurrentQueue<string>();

        private static readonly Timer healthCheckTimer = new Timer();
        private static bool isHealthCheckTimerActive = false;
        private static readonly object _lock = new object();

        public static Config GlobalConfig;

        public static IHost App { get; private set; }

        public static void Main(string[] args)
        {
            // Create a logger to enable logging also here in Program.cs (see https://stackoverflow.com/a/62404676/6237448)
            var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
            _logger = loggerFactory.CreateLogger<Startup>();

            // Load devices
            /*if (System.IO.File.Exists(Config.DEVICE_PATH))
            {
                try
                {
                    Devices = Serialization.Serialization.Read<List<Device>>(Config.DEVICE_PATH, Serialization.Serialization.Mode.Normal);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to read devices.xml: {ex.Message}");
                }

                if (Devices == null)
                    Devices = new List<Device>();
            }*/

            // Initalize config.json to GlobalConfig
            // Read config json (if any) [https://stackoverflow.com/a/28700387/6237448]
            string configPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "config.json");
            if (System.IO.File.Exists(configPath))
            {
                try
                {
                    string configJson = System.IO.File.ReadAllText(configPath);
                    GlobalConfig = System.Text.Json.JsonSerializer.Deserialize<Config>(configJson);
                    _logger.LogInformation("Successfully initalized config file!");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to read logfile: {ex.Message}");
                }
            }
            else
            {
                _logger.LogInformation("No config file found ...");
                GlobalConfig = new Config();
            }

            // Initalize health check timer
            healthCheckTimer.Interval = GlobalConfig.HealthCheckTimerInterval.TotalMilliseconds;
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

            // [deprecated, replaced with background service]

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
                    webBuilder.UseUrls(GlobalConfig.HostUrl);
                });
    }
}