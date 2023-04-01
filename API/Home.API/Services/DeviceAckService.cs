using Home.API.Helper;
using Home.API.home;
using Home.API.home.Models;
using Home.Data;
using Home.Data.Com;
using Home.Data.Events;
using Home.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Home.API.Services
{
    public interface IDeviceAckService
    {
        Task<DeviceAckServiceResult> ProcessDeviceAckAsync(Home.Model.Device device);
    }

    public class DeviceAckService : IDeviceAckService
    {
        private readonly ILogger<DeviceAckService> _logger;
        private readonly IClientService _clientService;
        private readonly HomeContext _context;

        public DeviceAckService(ILogger<DeviceAckService> logger, IClientService clientService, HomeContext context)
        {
            _logger = logger;
            _clientService = clientService;
            _context = context;
        }

        public async Task<DeviceAckServiceResult> ProcessDeviceAckAsync(Home.Model.Device requestedDevice)
        {
            var now = DateTime.Now;
            bool result = false;
            bool isScreenshotRequired = false;
            home.Models.DeviceMessage hasMessage = null;
            home.Models.DeviceCommand hasCommand = null;

            try
            {
                var currentDevice = await DeviceHelper.GetDeviceByIdAsync(_context, requestedDevice.ID);
                if (currentDevice != null)
                {
                    // Check if device was previously offline
                    if (currentDevice.Status == false)
                    {
                        currentDevice.Status = true;
                        currentDevice.IsScreenshotRequired = true;
                        currentDevice.LastSeen = now;
                        requestedDevice.LastSeen = now;

                        // Check for clearing usage stats (if the device was offline for more than one hour)
                        // Usage is currently not available to configure, because it is fixed to one hour!
                        if (currentDevice.LastSeen.AddHours(1) < DateTime.Now)
                            requestedDevice.Usage.Clear();

                        // Only notify webhook for server(s) and single board devices!
                        bool notifyWebhook = (requestedDevice.Type == Home.Model.Device.DeviceType.SingleBoardDevice || requestedDevice.Type == Home.Model.Device.DeviceType.Server);
                        var logEntry = ModelConverter.CreateLogEntry(currentDevice, $"Device \"{currentDevice.Name}\" has recovered and is now online again!", LogEntry.LogLevel.Information, notifyWebhook);
                        await _context.DeviceLog.AddAsync(logEntry);
                    }
                    else
                    {
                        currentDevice.LastSeen = now;
                        requestedDevice.LastSeen = now;
                    }

                    // If there is a change, append it in the log and notify webhook
                    await DetectAnyDevicesChangesAndLogAsync(currentDevice, requestedDevice, now);

                    isScreenshotRequired = currentDevice.IsScreenshotRequired;

                    // If this device is live, ALWAYS send a screenshot on ack!
                    if (currentDevice.IsLive == true)
                        isScreenshotRequired = true;

                    // Update device properties
                    await UpdateDeviceAsync(currentDevice, requestedDevice, now);

                    // Check for any storage warnings
                    await CheckForStorageWarningAsync(currentDevice, requestedDevice);

                    if (currentDevice.DeviceMessage.Count != 0)
                    {
                        if (currentDevice.DeviceMessage.Any(p => !p.IsRecieved))
                            hasMessage = currentDevice.DeviceMessage.Where(p => !p.IsRecieved).OrderBy(m => m.Timestamp).FirstOrDefault();

                        if (hasMessage != null)
                            hasMessage.IsRecieved = true;
                    }

                    if (hasMessage == null)
                    {
                        // Check for commands
                        if (currentDevice.DeviceCommand.Any(p => !p.IsExceuted))
                            hasCommand = currentDevice.DeviceCommand.Where(p => !p.IsExceuted).OrderBy(c => c.Timestamp).FirstOrDefault();

                        if (hasCommand != null)
                            hasCommand.IsExceuted = true;
                    }

                    result = true;
                    await _context.SaveChangesAsync();

                    _clientService.NotifyClientQueues(EventQueueItem.EventKind.ACK, currentDevice);
                }
                else
                {
                    // Temporary fix if data is empty again :(
                    requestedDevice.LastSeen = now;
                    await _context.Device.AddAsync(ModelConverter.ConvertDevice(_context, _logger, requestedDevice));
                    await _context.SaveChangesAsync();

                    _clientService.NotifyClientQueues(EventQueueItem.EventKind.NewDeviceConnected, requestedDevice);
                }

                if (result)
                {
                    // isScreenshotRequired
                    AckResult ackResult = new AckResult();
                    AckResult.Ack ack = AckResult.Ack.OK;

                    if (isScreenshotRequired)
                        ack |= AckResult.Ack.ScreenshotRequired;

                    if (hasMessage != null)
                    {
                        ack |= AckResult.Ack.MessageRecieved;
                        ackResult.JsonData = JsonConvert.SerializeObject(ModelConverter.ConvertMessage(hasMessage));
                    }

                    if (hasCommand != null)
                    {
                        ack |= AckResult.Ack.CommandRecieved;
                        ackResult.JsonData = JsonConvert.SerializeObject(ModelConverter.ConvertCommand(hasCommand));
                    }

                    ackResult.Result = ack;

                    // Reset error in case
                    if (Program.AckErrorSentAssoc.ContainsKey(requestedDevice.ID))
                        Program.AckErrorSentAssoc[requestedDevice.ID] = false;

                    return DeviceAckServiceResult.BuildSuccess(ackResult);
                }

                return DeviceAckServiceResult.BuildFailure("Device-ACK couldn't be processed. This device was not logged in before!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to process device ack: {ex}");

                // Only log once for a device
                bool send = false;
                if (!Program.AckErrorSentAssoc.ContainsKey(requestedDevice.ID))
                {
                    Program.AckErrorSentAssoc.Add(requestedDevice.ID, true);
                    send = true;
                }
                else
                {
                    if (!Program.AckErrorSentAssoc[requestedDevice.ID])
                    {
                        Program.AckErrorSentAssoc[requestedDevice.ID] = true;
                        send = true;
                    }
                }

                // Send a notification once
                if (send && Program.GlobalConfig.UseWebHook)
                    await Program.WebHook.PostWebHookAsync(WebhookAPI.Webhook.LogLevel.Error, $"ACK-ERROR [{requestedDevice.Name}] OCCURED: {ex.ToString()}", "DeviceAckService");

                return DeviceAckServiceResult.BuildFailure(ex.ToString());
            }
        }

        #region Update Device
        
        private async Task UpdateDeviceAsync(home.Models.Device currentDevice, Home.Model.Device requestedDevice, DateTime now)
        {
            // Update device usage
            await UpdateDeviceUsageAsync(currentDevice, requestedDevice);

            // Update device finally
            ModelConverter.UpdateDevice(_logger, _context, currentDevice, requestedDevice, Model.Device.DeviceStatus.Active, now);
        }

        #endregion

        #region Device Warnings

        private async Task CheckForBatteryWarningAsync(home.Models.Device currentDevice, Home.Model.Device requestedDevice)
        {
            if (requestedDevice.BatteryInfo.BatteryLevelInPercent <= Program.GlobalConfig.BatteryWarningPercentage)
            {
                var batteryWarning = currentDevice.DeviceWarning.Where(p => p.WarningType == (int)WarningType.BatteryWarning).FirstOrDefault();
                if (batteryWarning == null)
                {
                    // Create battery warning
                    var warning = BatteryWarning.Create((int)currentDevice.Environment.Battery.Percentage);
                    currentDevice.DeviceWarning.Add(new DeviceWarning() { WarningType = (int)Home.Model.WarningType.BatteryWarning, CriticalValue = (int)warning.Value, Timestamp = DateTime.Now });
                    await _context.DeviceLog.AddAsync(ModelConverter.ConvertLogEntry(currentDevice, warning.ConvertToLogEntry(currentDevice.Name)));
                }
                else
                {
                    // Update battery warning (no log due to the fact that the warning should be displayed in the gui)
                    batteryWarning.CriticalValue = (int)currentDevice.Environment.Battery.Percentage;
                }
            }
        }

        private async Task CheckForStorageWarningAsync(home.Models.Device currentDevice, Home.Model.Device requestedDevice)
        {
            if (requestedDevice.DiskDrives.Count > 0)
            {
                var dds = requestedDevice.DiskDrives.Where(d =>
                {
                    var result = d.IsFull(Program.GlobalConfig.StorageWarningPercentage);
                    return result.HasValue && result.Value;
                }).ToList();

                // Add storage warning
                // But ensure that the warning is only once per device and will be added again if dismissed by the user
                if (dds.Any())
                {
                    foreach (var disk in dds)
                    {
                        // Check for already existing storage warnings       
                        bool foundStorageWarning = false;
                        foreach (var sw in currentDevice.DeviceWarning.Where(p => p.WarningType == (int)WarningType.StorageWarning && !string.IsNullOrEmpty(p.AdditionalInfo)))
                        {
                            string[] entries = sw.AdditionalInfo.Split(new string[] { Consts.StatsSeperator }, StringSplitOptions.RemoveEmptyEntries);

                            if (sw.AdditionalInfo.Contains(Consts.StatsSeperator) && disk.UniqueID == entries.FirstOrDefault())
                            {
                                // Refresh this storage warning (if freeSpace changed)                                        
                                if (sw.CriticalValue != (long)disk.FreeSpace)
                                    sw.CriticalValue = (long)disk.FreeSpace;

                                foundStorageWarning = true;
                                break;
                            }
                        }

                        if (!foundStorageWarning)
                        {
                            // Add storage warning
                            var warning = StorageWarning.Create(disk.ToString(), disk.UniqueID, disk.FreeSpace);
                            currentDevice.DeviceWarning.Add(new DeviceWarning()
                            {
                                CriticalValue = (long)warning.Value,
                                Timestamp = DateTime.Now,
                                WarningType = (int)WarningType.StorageWarning,
                                AdditionalInfo = $"{disk.UniqueID}{Consts.StatsSeperator}{disk.DiskName}"
                            });

                            var logEntry = ModelConverter.ConvertLogEntry(currentDevice, warning.ConvertToLogEntry(requestedDevice.Name));
                            await _context.DeviceLog.AddAsync(logEntry);
                        }
                    }
                }
            }
        }

        #endregion

        #region Usage

        private async Task UpdateDeviceUsageAsync(home.Models.Device currentDevice, Home.Model.Device requestedDevice)
        {
            // USAGE
            // CPU & DISK
            if (currentDevice.DeviceUsage == null)
                currentDevice.DeviceUsage = new home.Models.DeviceUsage();

            currentDevice.DeviceUsage.Cpu = AddUsage(currentDevice.DeviceUsage.Cpu, currentDevice.Environment.Cpuusage);
            currentDevice.DeviceUsage.Disk = AddUsage(currentDevice.DeviceUsage.Disk, currentDevice.Environment.DiskUsage);

            // RAM
            var ram = currentDevice.Environment.FreeRam.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (ram != null && double.TryParse(ram, out double res))
                currentDevice.DeviceUsage.Ram = AddUsage(currentDevice.DeviceUsage.Ram, res);

            // Battery (if any) and also check for battery warning
            if (currentDevice.Environment.Battery != null)
            {
                currentDevice.DeviceUsage.Battery = AddUsage(currentDevice.DeviceUsage.Battery, currentDevice.Environment.Battery?.Percentage);
                await CheckForBatteryWarningAsync(currentDevice, requestedDevice);
            }
        }

        private string AddUsage(string data, double? usage)
        {
            if (usage == null)
                return data;

            if (string.IsNullOrEmpty(data))
                return $"{usage}{Consts.StatsSeperator}";

            List<string> values = data.Split(new string[] { Consts.StatsSeperator }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (values.Count + 1 > 60)
                values.RemoveAt(0);

            values.Add(usage.ToString());

            return string.Join(Consts.StatsSeperator, values);
        }
        #endregion

        #region Logging and Webhook

        private async Task RegisterDeviceChangeAsync(string prefix, string message, DeviceChangeEntry.DeviceChangeType type, home.Models.Device device, DateTime now)
        {
            if (type != DeviceChangeEntry.DeviceChangeType.None)            
            {
                string chgMessage = $"Detected {message}";
                await _context.DeviceChange.AddAsync(new DeviceChange() { Device = device, Description = chgMessage, Type = (int)type, Timestamp = now });
            }

            await _context.DeviceLog.AddAsync(ModelConverter.CreateLogEntry(device, $"{prefix} detected {message}", LogEntry.LogLevel.Information, true));

            _logger.LogInformation(message);
        }

        private async Task DetectAnyDevicesChangesAndLogAsync(home.Models.Device currentDevice, Home.Model.Device requestedDevice, DateTime now)
        {
            // Detect any device changes and log them (also log to Webhook)            
            string prefix = $"Device \"{requestedDevice.Name}\"";

            // Check if a newer client version is used
            if (currentDevice.ServiceClientVersion != requestedDevice.ServiceClientVersion && !string.IsNullOrEmpty(currentDevice.ServiceClientVersion))
                await RegisterDeviceChangeAsync(prefix, $"new client version: {requestedDevice.ServiceClientVersion} (Old version: {currentDevice.ServiceClientVersion})", DeviceChangeEntry.DeviceChangeType.None, currentDevice, now);           

            // Check if os name changed
            if (currentDevice.OstypeNavigation.Name != requestedDevice.OS.ToString())
                await RegisterDeviceChangeAsync(prefix, $"OS change from {currentDevice.OstypeNavigation.Name} to {requestedDevice.OS}", DeviceChangeEntry.DeviceChangeType.OS, currentDevice, now);

            // Check if os version changed (don't ignore updates in general)
            if (currentDevice.Environment.Osversion != requestedDevice.Environment.OSVersion && !string.IsNullOrEmpty(currentDevice.Environment.Osversion))
                await RegisterDeviceChangeAsync(prefix, $"new os version: {requestedDevice.Environment.OSVersion} (Old version: {currentDevice.Environment.Osversion})", DeviceChangeEntry.DeviceChangeType.OS, currentDevice, now);

            // CPU
            if (currentDevice.Environment.Cpuname != requestedDevice.Environment.CPUName && !string.IsNullOrEmpty(requestedDevice.Environment.CPUName))
                await RegisterDeviceChangeAsync(prefix, $"CPU change. CPU {currentDevice.Environment.Cpuname} got replaced with {requestedDevice.Environment.CPUName}", DeviceChangeEntry.DeviceChangeType.CPU,  currentDevice, now);

            // CPU Count
            if (currentDevice.Environment.Cpucount != requestedDevice.Environment.CPUCount && requestedDevice.Environment.CPUCount > 0)
                await RegisterDeviceChangeAsync(prefix, $"CPU-Count change from {currentDevice.Environment.Cpucount} to {requestedDevice.Environment.CPUCount}", DeviceChangeEntry.DeviceChangeType.CPU, currentDevice, now);

            // Motherboard
            if (currentDevice.Environment.Motherboard != requestedDevice.Environment.Motherboard && !string.IsNullOrEmpty(requestedDevice.Environment.Motherboard))
                await RegisterDeviceChangeAsync(prefix, $"Motherboard change from {currentDevice.Environment.Motherboard} to {requestedDevice.Environment.Motherboard}", DeviceChangeEntry.DeviceChangeType.Motherboard, currentDevice, now);

            // Graphics
            await NotifyWebhookGraphicsChangeAsync(prefix, currentDevice, requestedDevice, now);

            // RAM
            if (currentDevice.Environment.TotalRam != requestedDevice.Environment.TotalRAM && requestedDevice.Environment.TotalRAM > 0)
                await RegisterDeviceChangeAsync(prefix, $"RAM change from {currentDevice.Environment.TotalRam} GB to {requestedDevice.Environment.TotalRAM} GB", DeviceChangeEntry.DeviceChangeType.RAM, currentDevice, now);

            // IP Change
            if (currentDevice.Ip.Replace("/24", string.Empty) != requestedDevice.IP.Replace("/24", string.Empty) && !string.IsNullOrEmpty(requestedDevice.IP))
                await RegisterDeviceChangeAsync(prefix, $"IP change from {currentDevice.Ip} to {requestedDevice.IP}", DeviceChangeEntry.DeviceChangeType.IP, currentDevice, now);

            // Screens
            bool screenConfigChanged = false;

            // ToDo: *** Check if the screen itself changes? e.g. check if they are swapped?
            // currently the notification will only be triggered if a screen will be added/removed
            foreach (var screen in requestedDevice.Screens)
            {
                if (!currentDevice.DeviceScreen.Any(p => p.ScreenId == screen.ID))
                {
                    screenConfigChanged = true;
                    break;
                }
            }

            foreach (var screen in currentDevice.DeviceScreen)
            {
                if (!requestedDevice.Screens.Any(p => p.ID == screen.ScreenId))
                {
                    screenConfigChanged = true;
                    break;
                }
            }

            if (screenConfigChanged)
            {
                // Require also a new screenshot
                currentDevice.IsScreenshotRequired = true;

                if (Program.GlobalConfig.DetectScreenChangesAsDeviceChange)
                {
                    string oldScreens = string.Join(Environment.NewLine, currentDevice.DeviceScreen.Select(p => p.DeviceName));
                    string newScreens = string.Join(Environment.NewLine, requestedDevice.Screens.Select(p => p.DeviceName));

                    // Do not count this as a device change (only log)!
                    if (oldScreens.Trim() != newScreens.Trim())
                        await RegisterDeviceChangeAsync(prefix, $"screen changes.\n\nOld screen(s):\n{oldScreens}\n\nNew screen(s):\n{newScreens}", DeviceChangeEntry.DeviceChangeType.None, currentDevice, now);
                }
            }
        }

        private async Task NotifyWebhookGraphicsChangeAsync(string prefix, home.Models.Device currentDevice, Home.Model.Device requestedDevice, DateTime now)
        {
            bool log = false;
            if (currentDevice.DeviceGraphic.Count != requestedDevice.Environment.GraphicCards.Count)
            {
                #pragma warning disable CS0612
                if (requestedDevice.Environment.GraphicCards.Count == 0 && !string.IsNullOrEmpty(requestedDevice.Environment.Graphics))
                {
                    // ignore
                    log = false;
                }
                else
                    log = true;
                #pragma warning restore CS0612
            }
            else
            {
                // Count is equal, but in case the device have only one card (which is mostly the case), this one card could be changed
                foreach (var card in requestedDevice.Environment.GraphicCards)
                {
                    if (currentDevice.DeviceGraphic.Any(gc => gc.Name == card))
                        continue;

                    log = true;
                    break;
                }
            }

            if (log)
            {
                string oldCards = string.Join(Environment.NewLine, currentDevice.DeviceGraphic.Select(p => p.Name));
                string newCards = string.Join(Environment.NewLine, requestedDevice.Environment.GraphicCards);

                await RegisterDeviceChangeAsync(prefix, $"graphics change:\n\nOld card(s):\n {oldCards}\n\nNew card(s):\n {newCards}", DeviceChangeEntry.DeviceChangeType.Graphics, currentDevice, now);
            }
        }
        #endregion
    }

    public class DeviceAckServiceResult
    {
        public bool Success { get; set; }

        public AckResult AckResult { get; set; }

        public string ErrorMessage { get; set;}

        public int StatusCode { get; set; }

        public static DeviceAckServiceResult BuildSuccess(AckResult ackResult)
        {
            return new DeviceAckServiceResult()
            {
                AckResult = ackResult,
                StatusCode = StatusCodes.Status200OK,
                Success = true
            };
        }

        public static DeviceAckServiceResult BuildFailure(string errorMessage)
        {
            return new DeviceAckServiceResult()
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ErrorMessage = errorMessage
            };
        }
    }
}