using Home.API.home;
using Home.API.home.Models;
using Home.Data.Helper;
using Home.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Home.API.Services
{
    public class DeviceScheduleService : BackgroundService
    {
        private ILogger<DeviceScheduleService> _logger;
        private readonly IWOLService _wolService;
        private readonly IServiceScopeFactory _serviceProvider;
        private static DateTime lastServiceExecutionTime = DateTime.MinValue;

        public static bool UpdateSchedulingRules = false;
        private static IEnumerable<DeviceSchedulingRule> cache = null;

        public DeviceScheduleService(ILogger<DeviceScheduleService> logger, IWOLService wolService, IServiceScopeFactory serviceProvider) 
        {
            _logger = logger;
            _wolService = wolService;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay((int)TimeSpan.FromSeconds(0.25).TotalMilliseconds);

                    var now = DateTime.Now;
                    // Test 
                    // now = new DateTime(2023, 1, 30, 7, 55, 0);

                    // Make sure the service only runs once in a minute to prevent executing events multiple times!
                    if (lastServiceExecutionTime != DateTime.MinValue && lastServiceExecutionTime.Minute == now.Minute)
                        continue;
                    lastServiceExecutionTime = now;

                    var scope = _serviceProvider.CreateAsyncScope();
                    var _context = scope.ServiceProvider.GetService<HomeContext>();

                    if (UpdateSchedulingRules || cache == null)
                    {
                        cache = DeviceSchedulingRule.Load(Program.DeviceSchedulingRulesPath);
                        UpdateSchedulingRules = false;
                    }

                    foreach (var rule in cache)
                    {
                        if (!rule.IsActive) continue;
                        if (rule.BootRule == null && rule.ShutdownRule == null) continue;

                        if (rule.BootRule.Type != BootRule.BootRuleType.None)
                        {
                            var time = ParseTime(rule.BootRule.Time);
                            if (time == (-1, -1))
                            {
                                _logger.LogWarning("Ignoring scheduling rule due to invalid time ...");
                                continue;
                            }
                            else
                            {
                                if (time.Hour == now.Hour && time.Minute == now.Minute)
                                {
                                    if (rule.BootRule.Type == BootRule.BootRuleType.WakeOnLan)
                                        await ExecuteWakeOnLanAsync(_context, rule.AssociatedDeviceId, rule.CustomMacAddress);
                                    else if (rule.BootRule.Type == BootRule.BootRuleType.ExternalAPICall)
                                        await ExecuteExternalAPICallAsync(_context, rule.AssociatedDeviceId, "boot", rule.BootRule.RuleAPICallInfo.Url, rule.BootRule.RuleAPICallInfo.HttpMethod);
                                }
                            }
                        }

                        if (rule.ShutdownRule.Type != ShutdownRule.ShutdownRuleType.None)
                        {
                            var time = ParseTime(rule.ShutdownRule.Time);
                            if (time == (-1, -1))
                            {
                                _logger.LogWarning("Ignoring scheduling rule due to invalid time ...");
                                continue;
                            }
                            else
                            {
                                if (time.Hour == now.Hour && time.Minute == now.Minute)
                                {
                                    if (rule.ShutdownRule.Type == ShutdownRule.ShutdownRuleType.ExecuteCommand)
                                        await ExecuteCommandAsync(_context, rule.AssociatedDeviceId, rule.ShutdownRule.RuleCommandInfo.Executable, rule.ShutdownRule.RuleCommandInfo.Parameter);
                                    else if (rule.ShutdownRule.Type == ShutdownRule.ShutdownRuleType.Shutdown)
                                        await ExecuteShutdownDeviceAsync(_context, rule.AssociatedDeviceId, false);
                                    else if (rule.ShutdownRule.Type == ShutdownRule.ShutdownRuleType.Reboot)
                                        await ExecuteShutdownDeviceAsync(_context, rule.AssociatedDeviceId, true);
                                    else if (rule.ShutdownRule.Type == ShutdownRule.ShutdownRuleType.ExternalAPICall)
                                        await ExecuteExternalAPICallAsync(_context, rule.AssociatedDeviceId, "shutdown", rule.ShutdownRule.RuleAPICallInfo.Url, rule.ShutdownRule.RuleAPICallInfo.HttpMethod);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is TaskCanceledException))
                        _logger.LogError($"Error while executing DeviceScheduleService: {ex.Message}");
                }
            }
        }

        #region Actions

        /// <summary>
        /// Executes a command for a device given the deviceId, executable and parameter
        /// </summary>
        /// <param name="_context">The db context</param>
        /// <param name="deviceId">The device which will receive the command</param>
        /// <param name="executable">The executable to run on the device</param>
        /// <param name="parameter">The parameters to run the process</param>
        private async Task ExecuteCommandAsync(HomeContext _context, string deviceId, string executable, string parameter)
        {
            try
            {
                _logger.LogInformation($"Executing scheduled command {executable} {parameter} for device {deviceId} ...");

                var device = await _context.Device.Include(d => d.DeviceCommand).FirstOrDefaultAsync(d => d.Guid == deviceId);
                device.DeviceCommand.Add(new home.Models.DeviceCommand() { Device = device, Executable = executable, Parameter = parameter, Timestamp = DateTime.Now });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to add command {executable} with params {parameter}: {ex}");
            }
        }

        /// <summary>
        /// Wakes up the given host
        /// </summary>
        /// <param name="_context">The db context</param>
        /// <param name="deviceId">The device id which will receive the magic package</param>
        /// <param name="customMacAddress">Optional overridden mac address (e.g. if device mac is not set)</param>
        private async Task ExecuteWakeOnLanAsync(HomeContext _context, string deviceId, string customMacAddress)
        {           
            string macAddress;
            if (string.IsNullOrEmpty(customMacAddress))
                macAddress = (await _context.Device.FirstOrDefaultAsync(d => d.Guid == deviceId))?.MacAddress;
            else
                macAddress = customMacAddress;

            _logger.LogInformation($"Executing WOL for mac address: {customMacAddress} ...");
            await _wolService.SendWOLRequestAsync(macAddress);
        }

        /// <summary>
        /// Executes an external api call.
        /// </summary>
        /// <param name="_context">The db context</param>
        /// <param name="deviceId">The device id</param>
        /// <param name="action">The action (e.g. boot or shutdown)</param>
        /// <param name="url">The API Url</param>
        /// <param name="httpMethod">GET or POST</param>
        private async Task ExecuteExternalAPICallAsync(HomeContext _context, string deviceId, string action, string url, RuleAPICallInfo.Method httpMethod)
        {
            using (HttpClient client = new HttpClient()) 
            {
                try
                {
                    _logger.LogInformation($"Executing external API call: {url} ...");

                    if (httpMethod == RuleAPICallInfo.Method.GET)
                        await client.GetAsync(url);
                    else
                    {
                        var device = await _context.Device.FirstOrDefaultAsync(d => d.Guid == deviceId);
                        ArgumentNullException.ThrowIfNull(deviceId);

                        // POST
                        JObject shortDevice = new JObject()
                        {
                            ["id"] = device.Id,
                            ["name"] = device.Name,
                            ["ip"] = device.Ip,
                            ["mac"] = device.MacAddress,
                            ["action"] = action,
                        };

                        StringContent stringContent = new StringContent(shortDevice.ToString(Newtonsoft.Json.Formatting.None), System.Text.Encoding.UTF8, "application/json"); 
                        await client.PostAsync(url, stringContent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to execute external API call: {url}: {ex}");
                }
            }
        }

        /// <summary>
        /// Executes a shutdown or restart for a given device
        /// </summary>
        /// <param name="_context">The db context</param>
        /// <param name="deviceId">The device id for which the command should be executed</param>
        /// <param name="restart">true if restart; false for shutdown</param>
        private async Task ExecuteShutdownDeviceAsync(HomeContext _context, string deviceId, bool restart)
        {
            try
            {
                var device = await _context.Device.FirstOrDefaultAsync(d => d.Guid == deviceId);
                ArgumentNullException.ThrowIfNull(deviceId);

                var type = (Home.Model.Device.OSType)device.Ostype;
                if (type.IsAndroid())
                {
                    _logger.LogWarning($"Skipping executing of shutdown command: {device.Name} is an Android device and doesn't supports this command!");
                    return;
                }
                else if (!device.Status)
                {
                    _logger.LogWarning($"Skipping executing of shutdown command: {device.Name} is not active!");
                    return;
                }
                else
                {
                    var command = Home.API.Helper.GeneralHelper.GetShutdownCommand(restart, type);
                    await ExecuteCommandAsync(_context, deviceId, command.Item1, command.Item2);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to execute shutdown command: {ex.Message}!");
            }
        }

        #endregion

        private static (int Hour, int Minute) ParseTime(string dateTime)
        {
            if (string.IsNullOrEmpty(dateTime) || !dateTime.Contains(':') || dateTime.Length != 5)
                return (-1, -1);

            string[] parts = dateTime.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                return (-1, -1);

            try
            {
                return (int.Parse(parts[0]), int.Parse(parts[1]));
            }
            catch
            {
                return (-1, -1);
            }
        }
    }
}