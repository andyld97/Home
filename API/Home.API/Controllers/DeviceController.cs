using Home.API.Helper;
using Home.API.home;
using Home.API.home.Models;
using Home.Data;
using Home.Data.Com;
using Home.Data.Events;
using Home.Data.Helper;
using Home.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Home.Model.Device;
using Device = Home.Model.Device;

namespace Home.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly ILogger<DeviceController> _logger;
        private readonly HomeContext _context;

        public DeviceController(ILogger<DeviceController> logger, HomeContext homeContext)
        {
            _logger = logger;
            _context = homeContext;
        }


        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var devices = await ModelConverter.GetAllDevicesAsync(_context);

            List<Device> devicesList = new List<Device>();
            foreach (var device in devices)
                devicesList.Add(ModelConverter.ConvertDevice(device));

            return Ok(devicesList);
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Device device)
        {
            var now = DateTime.Now;

            if (device == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device given"));

            bool result = false;

            // Check if device exists
            if (await _context.Device.AnyAsync(p => p.Guid == device.ID))
            {
                device.Status = Device.DeviceStatus.Active;
                device.LastSeen = now;
                device.LogEntries.Clear();
                device.LogEntries.Add(new LogEntry(now, $"Device {device.Name} was successfully added!", LogEntry.LogLevel.Information));
                _logger.LogInformation($"New device {device.Environment.MachineName} has just logged in!");
                device.IsScreenshotRequired = true;

                var dbDevice = ModelConverter.ConvertDevice(_context, _logger, device);
                await _context.Device.AddAsync(dbDevice);

                ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.NewDeviceConnected, device);
                await _context.SaveChangesAsync();

                result = true;
            }

            if (result)
                return Ok(AnswerExtensions.Success(true));

            return BadRequest(AnswerExtensions.Fail("Device-Register couldn't be processed!"));
        }

        private string AddUsage(string data, double? usage)
        {
            if (usage == null)
                return data;

            if (string.IsNullOrEmpty(data))
                return $"{usage}|";

            List<string> values = data.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (values.Count + 1 > 60)
                values.RemoveAt(0);

            values.Add(usage.ToString());

            return string.Join("|", values);
        }

        [HttpPost("ack")]
        public async Task<IActionResult> AckAsnyc([FromBody] Home.Model.Device requestedDevice)
        {
            var now = DateTime.Now;

            if (requestedDevice == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device given"));

            bool result = false;
            bool isScreenshotRequired = false;
            home.Models.DeviceMessage hasMessage = null;
            home.Models.DeviceCommand hasCommand = null;

            try
            {
                var currentDevice = await ModelConverter.GetDeviceByIdAsync(_context, requestedDevice.ID);
                if (currentDevice != null)
                {
                    // Check if device was previously offline
                    if (currentDevice.Status == false) // Device.DeviceStatus.Offline)
                    {
                        currentDevice.Status = true;
                        currentDevice.IsScreenshotRequired = true;
                        currentDevice.LastSeen = now;
                        requestedDevice.LastSeen = now;

                        // Check for clearing usage stats (if the device was offline for more than one hour)
                        // Usage is currently not avaiable to configure, because it is fixed to one hour!
                        if (currentDevice.LastSeen.AddHours(1) < DateTime.Now)
                            requestedDevice.Usage.Clear();

                        var logEntry = ModelConverter.CreateLogEntry(currentDevice, $"Device \"{currentDevice.Name}\" has recovered and is now online again!", LogEntry.LogLevel.Information, (requestedDevice.Type == Device.DeviceType.SingleBoardDevice || requestedDevice.Type == Device.DeviceType.Server));
                        await _context.DeviceLog.AddAsync(logEntry);
                    }
                    else
                    {
                        currentDevice.LastSeen = now;
                        requestedDevice.LastSeen = now;
                    }

                    // Check if a newer client version is used
                    if (currentDevice.ServiceClientVersion != requestedDevice.ServiceClientVersion && !string.IsNullOrEmpty(currentDevice.ServiceClientVersion))
                    {
                        var logEntry = ModelConverter.CreateLogEntry(currentDevice, $"Device \"{requestedDevice.Name}\" detected new client version: {requestedDevice.ServiceClientVersion}", LogEntry.LogLevel.Information, true);
                        await _context.DeviceLog.AddAsync(logEntry);
                    }
                    // Detect any device changes and log them (also to Telegram)

                    // CPU
                    if (currentDevice.Environment.Cpuname != requestedDevice.Environment.CPUName && !string.IsNullOrEmpty(requestedDevice.Environment.CPUName))
                    {
                        var logEntry = ModelConverter.CreateLogEntry(currentDevice, $"Device \"{requestedDevice.Name}\" detected CPU change. CPU {currentDevice.Environment.Cpuname} got replaced with {requestedDevice.Environment.CPUName}", LogEntry.LogLevel.Information, true);
                        await _context.DeviceLog.AddAsync(logEntry);
                    }
                    if (currentDevice.Environment.Cpucount != requestedDevice.Environment.CPUCount && requestedDevice.Environment.CPUCount > 0)
                    {
                        var logEntry = ModelConverter.CreateLogEntry(currentDevice, $"Device \"{requestedDevice.Name}\" detected CPU-Count change from {currentDevice.Environment.Cpucount} to {requestedDevice.Environment.CPUCount}", LogEntry.LogLevel.Information, true);
                        await _context.DeviceLog.AddAsync(logEntry);
                    }

                    // OS (Ignore Windows Updates, just document enum chnages)
                    if (currentDevice.OstypeNavigation.Name != requestedDevice.OS.ToString())
                    {
                        var logEntry = ModelConverter.CreateLogEntry(currentDevice, $"Device \"{requestedDevice.Name}\" detected OS change from {currentDevice.OstypeNavigation.Name} to {requestedDevice.OS}", LogEntry.LogLevel.Information, true);
                        await _context.DeviceLog.AddAsync(logEntry);
                    }

                    // Motherboard
                    if (currentDevice.Environment.Motherboard != requestedDevice.Environment.Motherboard && !string.IsNullOrEmpty(requestedDevice.Environment.Motherboard))
                    {
                        var logEntry = ModelConverter.CreateLogEntry(currentDevice, $"Device \"{requestedDevice.Name}\" detected Motherboard change from {currentDevice.Environment.Motherboard} to {requestedDevice.Environment.Motherboard}", LogEntry.LogLevel.Information, true);
                        await _context.DeviceLog.AddAsync(logEntry);
                    }

                    // Graphics
                    //if (oldDevice.Envoirnment.Graphics != refreshedDevice.Envoirnment.Graphics && !string.IsNullOrEmpty(refreshedDevice.Envoirnment.Graphics))
                    //    oldDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected Graphics change from {oldDevice.Envoirnment.Graphics} to {refreshedDevice.Envoirnment.Graphics}", LogEntry.LogLevel.Information, true));
                    if (currentDevice.DeviceGraphic.Count != requestedDevice.Environment.GraphicCards.Count)
                    {
                        if (requestedDevice.Environment.GraphicCards.Count == 0 && !string.IsNullOrEmpty(requestedDevice.Environment.Graphics))
                        {
                            // ignore
                        }
                        else
                        {
                            foreach (var item in requestedDevice.Environment.GraphicCards)
                            {
                                var logEntry = ModelConverter.CreateLogEntry(currentDevice, $"Device \"{requestedDevice.Name}\" detected Graphics change(s) ({item})", LogEntry.LogLevel.Information, true);
                                await _context.DeviceLog.AddAsync(logEntry);
                            }
                        }
                    }

                    // RAM
                    if (currentDevice.Environment.TotalRam != requestedDevice.Environment.TotalRAM && requestedDevice.Environment.TotalRAM > 0)
                    {
                        var logEntry = ModelConverter.CreateLogEntry(currentDevice, $"Device \"{requestedDevice.Name}\" detected RAM change from {currentDevice.Environment.TotalRam} GB to {requestedDevice.Environment.TotalRAM} GB", LogEntry.LogLevel.Information, true);
                        await _context.DeviceLog.AddAsync(logEntry);
                    }

                    // IP Change
                    if (currentDevice.Ip.Replace("/24", string.Empty) != requestedDevice.IP.Replace("/24", string.Empty) && !string.IsNullOrEmpty(requestedDevice.IP))
                    {
                        var logEntry = ModelConverter.CreateLogEntry(currentDevice, $"Device \"{requestedDevice.Name}\" detected IP change from {currentDevice.Ip} to {requestedDevice.IP}", LogEntry.LogLevel.Information, true);
                        await _context.DeviceLog.AddAsync(logEntry);
                    }

                    isScreenshotRequired = currentDevice.IsScreenshotRequired;

                    // If this device is live, ALWAYS send a screenshot on ack!
                    if (currentDevice.IsLive.HasValue && currentDevice.IsLive.Value)
                        isScreenshotRequired = true;

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

                    // Battery (if any)
                    if (currentDevice.Environment.Battery != null)
                    {
                        currentDevice.DeviceUsage.Battery = AddUsage(currentDevice.DeviceUsage.Battery, currentDevice.Environment.Battery?.Percentage);

                        // Check for battery warning
                        if (requestedDevice.BatteryInfo.BatteryLevelInPercent <= Program.GlobalConfig.BatteryWarningPercentage)
                        {
                            var batteryWarning = currentDevice.DeviceWarning.Where(p => p.WarningType == (int)WarningType.BatteryWarning).FirstOrDefault();
                            if (batteryWarning == null)
                            {
                                // Create battery warning
                                var warning = BatteryWarning.Create((int)currentDevice.Environment.Battery.Percentage);
                                currentDevice.DeviceWarning.Add(new DeviceWarning() { WarningType = (int)Home.Model.WarningType.BatteryWarning, CriticalValue = (int)warning.Value, Timestamp = DateTime.Now });
                                await _context.DeviceLog.AddAsync(ModelConverter.ConvertLogEntry(currentDevice, warning.ConvertToLogEntry()));
                            }
                            else
                            {
                                // Update battery warning (no log due to the fact that the warning should be displayed in the gui)
                                batteryWarning.CriticalValue = (int)currentDevice.Environment.Battery.Percentage;
                            }
                        }
                    }

                    // Update device
                    ModelConverter.UpdateDevice(_logger, _context, currentDevice, requestedDevice, DeviceStatus.Active, now);

                    if (requestedDevice.DiskDrives.Count > 0)
                    {
                        var dds = requestedDevice.DiskDrives.Where(d =>
                        {
                            var result = d.IsFull(Program.GlobalConfig.StorageWarningPercentage);
                            return result.HasValue && result.Value;
                        }).ToList();

                        // Add storage warning
                        // But ensure that the warning is only once per device and will be added again if dismissed by the user
                        if (dds.Count > 0)
                        {
                            foreach (var disk in dds)
                            {
                                // Check for already existing storage warnings       
                                bool foundStorageWarning = false;
                                foreach (var sw in currentDevice.DeviceWarning.Where(p => p.WarningType == (int)WarningType.StorageWarning && !string.IsNullOrEmpty(p.AdditionalInfo)))
                                {
                                    string[] entries = sw.AdditionalInfo.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                                    if (sw.AdditionalInfo.Contains("|") && disk.UniqueID == entries.FirstOrDefault())
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
                                        AdditionalInfo = $"{disk.UniqueID}|{disk.DiskName}"
                                    });
                                    
                                    var logEntry = ModelConverter.ConvertLogEntry(currentDevice, warning.ConvertToLogEntry());
                                    await _context.DeviceLog.AddAsync(logEntry);
                                }
                            }
                        }
                    }

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

                    ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.ACK, currentDevice);
                }
                else
                {
                    // Temporay fix if data is empty again :(
                    requestedDevice.LastSeen = now;
                    await _context.Device.AddAsync(ModelConverter.ConvertDevice(_context, _logger, requestedDevice));
                    await _context.SaveChangesAsync();

                    ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.NewDeviceConnected, requestedDevice);
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
                    if (Program.AckErrorSentAssoc.ContainsKey(requestedDevice))
                        Program.AckErrorSentAssoc[requestedDevice] = false;

                    return Ok(AnswerExtensions.Success(ackResult));
                }

                return BadRequest(new Answer<AckResult>("fail", new AckResult(AckResult.Ack.Invalid)) { ErrorMessage = "Device-ACK couldn't be processed. This device was not logged in before!" });

            }
            catch (Exception ex)
            {
                // Only log once for a device
                _logger.LogError(ex.Message);

                bool send = false;
                if (!Program.AckErrorSentAssoc.ContainsKey(requestedDevice))
                {
                    Program.AckErrorSentAssoc.Add(requestedDevice, true);
                    send = true;
                }
                else
                {
                    if (!Program.AckErrorSentAssoc[requestedDevice])
                    {
                        Program.AckErrorSentAssoc[requestedDevice] = true;
                        send = true;
                    }
                }

                // Send a notification once
                if (send && Program.GlobalConfig.UseWebHook)
                    await WebHook.NotifyWebHookAsync(Program.GlobalConfig.WebHookUrl, $"ACK-ERROR OCCURED: {ex.ToString()}");

                return BadRequest(new Answer<AckResult>("fail", new AckResult(AckResult.Ack.Invalid)) { ErrorMessage = ex.ToString() });
            }
        }

        [HttpPost("screenshot")]
        public async Task<IActionResult> PostScreenshotAsync([FromBody] Screenshot shot)
        {
            var now = DateTime.Now;
            string fileName = now.ToString(Consts.SCREENSHOT_DATE_FILE_FORMAT);

            if (shot == null)
                return BadRequest(AnswerExtensions.Fail("screenshot is null!"));

            var deviceFound = await _context.GetDeviceByIdAsync(shot.DeviceID);

            if (deviceFound == null)
                return BadRequest(AnswerExtensions.Fail("Device not found!"));

            try
            {
                byte[] data = Convert.FromBase64String(shot.Data);

                using (Stream stream = new System.IO.MemoryStream(data))
                {
                    // Perform necessary actions with file stream
                    string clientPath = System.IO.Path.Combine(Config.SCREENSHOTS_PATH, deviceFound.Guid);

                    // Create folder
                    if (!System.IO.Directory.Exists(clientPath))
                        System.IO.Directory.CreateDirectory(clientPath);

                    string newPath = System.IO.Path.Combine(clientPath, $"{fileName}.png");

                    long bytes = 0;
                    byte[] buffer = new byte[4096];

                    using (System.IO.FileStream fs = new FileStream(newPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        while (bytes < stream.Length)
                        {
                            if (Math.Abs(bytes - stream.Length) < buffer.Length)
                                buffer = new byte[Math.Abs(bytes - stream.Length)];

                            bytes += await stream.ReadAsync(buffer, 0, buffer.Length);
                            await fs.WriteAsync(buffer, 0, buffer.Length);
                        }
                    }
                }

                // Add filename to list and append to log
                var logEntry = ModelConverter.CreateLogEntry(deviceFound, "Recieved screenshot from this device!", LogEntry.LogLevel.Information);
                await _context.DeviceLog.AddAsync(logEntry);
                _logger.LogInformation($"Recieved screenshot from {deviceFound.Environment.MachineName}");
                deviceFound.DeviceScreenshot.Add(new DeviceScreenshot() { Device = deviceFound, ScreenshotFileName = fileName, Timestamp = DateTime.Now });
                deviceFound.IsScreenshotRequired = false;

                // Also append to event queue
                ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.DeviceScreenshotRecieved, deviceFound);

                await _context.SaveChangesAsync();
                return Ok(AnswerExtensions.Success(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(AnswerExtensions.Fail(ex.Message));
            }
        }
    }
}