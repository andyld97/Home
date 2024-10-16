using Home.API.Helper;
using Home.API.home;
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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using WebhookAPI;
using static Home.API.Helper.ModelConverter;

namespace Home.API
{
    public class Program
    {
        private static ILogger _logger;
        public readonly static List<EventQueue> EventQueues = [];
        public readonly static List<Client> Clients = [];
        public readonly static Dictionary<Client, List<string>> LiveModeAssoc = [];
        public readonly static Dictionary<string, bool> AckErrorSentAssoc = [];
        public static ConcurrentQueue<(Webhook.LogLevel level, string message, string scope)> WebHookLogging = new ConcurrentQueue<(Webhook.LogLevel level, string message, string scope)>();

        public static Config GlobalConfig;
        public static string DeviceSchedulingRulesPath;

        public static IHost App { get; private set; }

        public static void Main(string[] args)
        {
            // Create a logger to enable logging also here in Program.cs (see https://stackoverflow.com/a/62404676/6237448)
            var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
            _logger = loggerFactory.CreateLogger<Startup>();

            // Initialize config.json to GlobalConfig
            // Read config json (if any)
            string configPath = System.IO.Path.Combine(AppContext.BaseDirectory, "config.json");
            DeviceSchedulingRulesPath = System.IO.Path.Combine(AppContext.BaseDirectory, "scheduling.json");

            if (System.IO.File.Exists(configPath))
            {
                try
                {
                    string configJson = System.IO.File.ReadAllText(configPath);
                    GlobalConfig = System.Text.Json.JsonSerializer.Deserialize<Config>(configJson);
                    _logger.LogInformation("Successfully initialized config file!");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to read config file: {ex.Message}");
                }
            }
            else
            {
                _logger.LogInformation("No config file found ...");
                GlobalConfig = new Config();
            }

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureLogging((ctx, builder) => 
                    {
                        builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                        builder.AddFile(o => o.RootPath = ctx.HostingEnvironment.ContentRootPath);
                    });

                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls(GlobalConfig.HostUrl);
                });
    }
}