using Home.API.Helper;
using Home.API.home;
using Home.API.home.Models;
using Home.API.Model;
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
        private readonly HomeContext _homeContext;

        public DeviceController(ILogger<DeviceController> logger, HomeContext homeContext)
        {
            _logger = logger;
            _homeContext = homeContext;
        }


        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var devices = await DeviceHelper.GetAllDevicesAsync(_homeContext);
            
            List<Device> devicesList = new List<Device>();
            foreach (var device in devices)
                devicesList.Add(DeviceHelper.ConvertDevice(device));

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
            if (await _homeContext.Device.AnyAsync(p => p.Guid == device.ID))
            {
                device.Status = Device.DeviceStatus.Active;
                device.LastSeen = now;
                device.LogEntries.Clear();
                device.LogEntries.Add(new LogEntry(now, $"Device {device.Name} was successfully added!", LogEntry.LogLevel.Information));
                _logger.LogInformation($"New device {device.Environment.MachineName} has just logged in!");
                device.IsScreenshotRequired = true;

                var dbDevice = await DeviceHelper.ConvertDeviceAsync(_homeContext, device);
                await _homeContext.Device.AddAsync(dbDevice);

                lock (Program.EventQueues)
                {
                    foreach (var queue in Program.EventQueues)
                    {
                        queue.LastEvent = now;
                        queue.Events.Enqueue(new EventQueueItem() { DeviceID = device.ID, EventData = new EventData(DeviceHelper.ConvertDevice(dbDevice)), EventDescription = EventQueueItem.EventKind.NewDeviceConnected, EventOccured = now });
                    }
                }

                // Write to database
                await _homeContext.SaveChangesAsync();
                result = true;
            }

            if (result)
                return Ok(AnswerExtensions.Success(true));

            return BadRequest(AnswerExtensions.Fail("Device-Register couldn't be processed!"));
        }

        [HttpPost("ack")]
        public async Task<IActionResult> AckAsnyc([FromBody] Home.Model.Device refreshedDevice)
        {
            var now = DateTime.Now;

            if (refreshedDevice == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device given"));

            bool result = false;
            bool isScreenshotRequired = false;
            Message hasMessage = null;
            Command hasCommand = null;

            try
            {
                var currentDevice = await DeviceHelper.GetDeviceByIdAsync(_homeContext, refreshedDevice.ID);
                if (currentDevice != null)
                {
                    // Check if device was previously offline
                    if (currentDevice.Status == false) // Device.DeviceStatus.Offline)
                    {
                        currentDevice.Status = true;

                        // Check for clearing usage stats (if the device was offline for more than one hour)
                        // Usage is currently not avaiable to configure, because it is fixed to one hour!
                        if (currentDevice.LastSeen.AddHours(1) < DateTime.Now)
                            refreshedDevice.Usage.Clear();

                        var logEntry = DeviceHelper.CreateLogEntry(currentDevice, $"Device \"{currentDevice.Name}\" has recovered and is now online again!", LogEntry.LogLevel.Information, (refreshedDevice.Type == Device.DeviceType.SingleBoardDevice || refreshedDevice.Type == Device.DeviceType.Server));
                        await _homeContext.DeviceLog.AddAsync(logEntry);

                        isScreenshotRequired = true;
                    }

                    // Check if a newer client version is used
                    if (currentDevice.ServiceClientVersion != refreshedDevice.ServiceClientVersion && !string.IsNullOrEmpty(currentDevice.ServiceClientVersion))
                    {
                        var logEntry = DeviceHelper.CreateLogEntry(currentDevice, $"Device \"{refreshedDevice.Name}\" detected new client version: {refreshedDevice.ServiceClientVersion}", LogEntry.LogLevel.Information, true);
                        await _homeContext.DeviceLog.AddAsync(logEntry);
                    }
                    // Detect any device changes and log them (also to Telegram)

                    // CPU
                    if (currentDevice.Environment.Cpuname != refreshedDevice.Environment.CPUName && !string.IsNullOrEmpty(refreshedDevice.Environment.CPUName))
                    {
                        var logEntry = DeviceHelper.CreateLogEntry(currentDevice, $"Device \"{refreshedDevice.Name}\" detected CPU change. CPU {currentDevice.Environment.Cpuname} got replaced with {refreshedDevice.Environment.CPUName}", LogEntry.LogLevel.Information, true);
                        await _homeContext.DeviceLog.AddAsync(logEntry);
                    }
                    if (currentDevice.Environment.Cpucount != refreshedDevice.Environment.CPUCount && refreshedDevice.Environment.CPUCount > 0)
                    {
                        var logEntry = DeviceHelper.CreateLogEntry(currentDevice, $"Device \"{refreshedDevice.Name}\" detected CPU-Count change from {currentDevice.Environment.Cpucount} to {refreshedDevice.Environment.CPUCount}", LogEntry.LogLevel.Information, true);
                        await _homeContext.DeviceLog.AddAsync(logEntry);
                    }

                    // OS (Ignore Windows Updates, just document enum chnages)
                    if (currentDevice.OstypeNavigation.Name != refreshedDevice.OS.ToString())
                    {
                        var logEntry = DeviceHelper.CreateLogEntry(currentDevice, $"Device \"{refreshedDevice.Name}\" detected OS change from {currentDevice.OstypeNavigation.Name} to {refreshedDevice.OS}", LogEntry.LogLevel.Information, true);
                        await _homeContext.DeviceLog.AddAsync(logEntry);
                    }

                    // Motherboard
                    if (currentDevice.Environment.Motherboard != refreshedDevice.Environment.Motherboard && !string.IsNullOrEmpty(refreshedDevice.Environment.Motherboard))
                    {
                        var logEntry = DeviceHelper.CreateLogEntry(currentDevice, $"Device \"{refreshedDevice.Name}\" detected Motherboard change from {currentDevice.Environment.Motherboard} to {refreshedDevice.Environment.Motherboard}", LogEntry.LogLevel.Information, true);
                        await _homeContext.DeviceLog.AddAsync(logEntry);
                    }

                    // Graphics
                    //if (oldDevice.Envoirnment.Graphics != refreshedDevice.Envoirnment.Graphics && !string.IsNullOrEmpty(refreshedDevice.Envoirnment.Graphics))
                    //    oldDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected Graphics change from {oldDevice.Envoirnment.Graphics} to {refreshedDevice.Envoirnment.Graphics}", LogEntry.LogLevel.Information, true));
                    if (currentDevice.DeviceGraphic.Count != refreshedDevice.Environment.GraphicCards.Count)
                    {
                        if (refreshedDevice.Environment.GraphicCards.Count == 0 && !string.IsNullOrEmpty(refreshedDevice.Environment.Graphics))
                        {
                            // ignore
                        }
                        else
                        {
                            foreach (var item in refreshedDevice.Environment.GraphicCards)
                            {
                                var logEntry = DeviceHelper.CreateLogEntry(currentDevice, $"Device \"{refreshedDevice.Name}\" detected Graphics change(s) ({item})", LogEntry.LogLevel.Information, true);
                                await _homeContext.DeviceLog.AddAsync(logEntry);
                            }
                        }
                    }

                    // RAM
                    if (currentDevice.Environment.TotalRam != refreshedDevice.Environment.TotalRAM && refreshedDevice.Environment.TotalRAM > 0)
                    {
                        var logEntry = DeviceHelper.CreateLogEntry(currentDevice, $"Device \"{refreshedDevice.Name}\" detected RAM change from {currentDevice.Environment.TotalRam} GB to {refreshedDevice.Environment.TotalRAM} GB", LogEntry.LogLevel.Information, true);
                        await _homeContext.DeviceLog.AddAsync(logEntry);
                    }

                    // IP Change
                    if (currentDevice.Ip.Replace("/24", string.Empty) != refreshedDevice.IP.Replace("/24", string.Empty) && !string.IsNullOrEmpty(refreshedDevice.IP))
                    {
                        var logEntry = DeviceHelper.CreateLogEntry(currentDevice, $"Device \"{refreshedDevice.Name}\" detected IP change from {currentDevice.Ip} to {refreshedDevice.IP}", LogEntry.LogLevel.Information, true);
                        await _homeContext.DeviceLog.AddAsync(logEntry);
                    }

                    isScreenshotRequired = refreshedDevice.IsScreenshotRequired;

                    // If this device is live, ALWAYS send a screenshot on ack!
                    if (currentDevice.IsLive.HasValue && currentDevice.IsLive.Value)
                        isScreenshotRequired = true;

                    // USAGE
                    // CPU & DISK

                    // ToDo: ***
                    // currentDevice.Usage.AddCPUEntry(refreshedDevice.Environment.CPUUsage);
                    // currentDevice.Usage.AddDISKEntry(refreshedDevice.Environment.DiskUsage);

                    // RAM

                    // ToDo: ***
                    // var ram = refreshedDevice.Environment.FreeRAM.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    // if (ram != null && double.TryParse(ram, out double res))
                    //    currentDevice.Usage.AddRAMEntry(res);

                    // Battery (if any)
                    if (refreshedDevice.BatteryInfo != null)
                    {
                        // ToDo: ***
                        // currentDevice.Usage.AddBatteryEntry(currentDevice.BatteryInfo.BatteryLevelInPercent);

                        // Check for battery warning
                        if (refreshedDevice.BatteryInfo.BatteryLevelInPercent <= Program.GlobalConfig.BatteryWarningPercentage)
                        {
                            // ToDo: ***
                            /*if (currentDevice.BatteryWarning != null)
                            {
                                // Create battery warning
                                var batteryWarning = BatteryWarning.Create(currentDevice.BatteryInfo.BatteryLevelInPercent);
                                currentDevice.BatteryWarning = batteryWarning;
                                currentDevice.LogEntries.Add(batteryWarning.ConvertToLogEntry());
                            }
                            else if (currentDevice.BatteryWarning == null)
                            {
                                // Update battery warning (no log due to the fact that the warning should be displayed in the gui)
                                currentDevice.BatteryWarning.Value = currentDevice.BatteryInfo.BatteryLevelInPercent;
                            }*/
                        }
                    }

                    // Update device
                    await DeviceHelper.UpdateDeviceAsync(_homeContext, currentDevice, refreshedDevice, DeviceStatus.Active, now);

                    if (refreshedDevice.DiskDrives.Count > 0)
                    {
                        var dds = refreshedDevice.DiskDrives.Where(d =>
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
                                /* ToDo: ***
                                // Check for already existing storage warnings       
                                if (currentDevice.StorageWarnings.Any(s => s.StorageID == disk.UniqueID))
                                {
                                    // Refresh this storage warning (if freeSpace changed)                                        
                                    var oldWarning = currentDevice.StorageWarnings.FirstOrDefault(sw => sw.StorageID == disk.UniqueID);
                                    if (oldWarning.Value != disk.FreeSpace)
                                        oldWarning.Value = disk.FreeSpace;
                                    continue;
                                }

                                // Add storage warning
                                var warning = StorageWarning.Create(disk.ToString(), disk.UniqueID, disk.FreeSpace);
                                currentDevice.StorageWarnings.Add(warning);
                                currentDevice.LogEntries.Add(warning.ConvertToLogEntry());
                                */
                            }
                        }
                    }

                    // ToDo: ***
                    /*lock (currentDevice.Messages)
                    {
                        if (currentDevice.Messages.Count != 0)
                            hasMessage = currentDevice.Messages.Dequeue();
                    }

                    if (hasMessage == null)
                    {
                        lock (currentDevice.Commands)
                        {
                            // Check for commands
                            if (currentDevice.Commands.Count != 0)
                                hasCommand = currentDevice.Commands.Dequeue();
                        }
                    }*/

                    result = true;
                    await _homeContext.SaveChangesAsync();

                    lock (Program.EventQueues)
                    {
                        foreach (var queue in Program.EventQueues)
                        {
                            queue.LastEvent = now;
                            queue.Events.Enqueue(new EventQueueItem() { DeviceID = currentDevice.Guid, EventData = new EventData() { EventDevice = DeviceHelper.ConvertDevice(currentDevice) }, EventDescription = EventQueueItem.EventKind.ACK, EventOccured = now });
                        }
                    }

                }
                else
                {
                    // Temporay fix if data is empty again :(
                    await _homeContext.Device.AddAsync(await DeviceHelper.ConvertDeviceAsync(_homeContext, refreshedDevice));
                    await _homeContext.SaveChangesAsync();

                    lock (Program.EventQueues)
                    {
                        foreach (var queue in Program.EventQueues)
                        {
                            queue.LastEvent = now;
                            queue.Events.Enqueue(new EventQueueItem() { DeviceID = refreshedDevice.ID, EventData = new EventData() { EventDevice = refreshedDevice }, EventDescription = EventQueueItem.EventKind.NewDeviceConnected, EventOccured = now });
                        }
                    }
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
                        ackResult.JsonData = JsonConvert.SerializeObject(hasMessage);
                    }

                    if (hasCommand != null)
                    {
                        ack |= AckResult.Ack.CommandRecieved;
                        ackResult.JsonData = JsonConvert.SerializeObject(hasCommand);
                    }

                    ackResult.Result = ack;

                    // Reset error in case
                    if (Program.AckErrorSentAssoc.ContainsKey(refreshedDevice))
                        Program.AckErrorSentAssoc[refreshedDevice] = false;

                    return Ok(AnswerExtensions.Success(ackResult));
                }

                return BadRequest(new Answer<AckResult>("fail", new AckResult(AckResult.Ack.Invalid)) { ErrorMessage = "Device-ACK couldn't be processed. This device was not logged in before!" });

            }
            catch (Exception ex)
            {
                // Only log once for a device
                _logger.LogError(ex.Message);

                bool send = false;
                if (!Program.AckErrorSentAssoc.ContainsKey(refreshedDevice))
                {
                    Program.AckErrorSentAssoc.Add(refreshedDevice, true);
                    send = true;
                }
                else
                {
                    if (!Program.AckErrorSentAssoc[refreshedDevice])
                    {
                        Program.AckErrorSentAssoc[refreshedDevice] = true;
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
        public async Task<IActionResult> PostScreenshot([FromBody] Screenshot shot)
        {
            // ToDo:

            return Ok();

            /* var now = DateTime.Now;
             string fileName = now.ToString(Consts.SCREENSHOT_DATE_FILE_FORMAT);

             if (shot == null)
                 return BadRequest(AnswerExtensions.Fail("screenshot is null!"));

             Device deviceFound = null;
             lock (Program.Devices)
             {
                 // Program.Devices.shot.ClientID
                 if (Program.Devices.Any(p => p.ID == shot.ClientID))
                     deviceFound = Program.Devices.Where(p => p.ID == shot.ClientID).FirstOrDefault();
             }

             if (deviceFound == null)
                 return BadRequest(AnswerExtensions.Fail("Device not found!"));

             try
             {
                 byte[] data = Convert.FromBase64String(shot.Data);

                 using (Stream stream = new System.IO.MemoryStream(data))
                 {
                     // Perform necessary actions with file stream
                     string clientPath = System.IO.Path.Combine(Config.SCREENSHOTS_PATH, deviceFound.ID);

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
                 lock (Program.Devices)
                 {
                     deviceFound.LogEntries.Add(new LogEntry(now, "Recieved screenshot from this device!", LogEntry.LogLevel.Information));
                     _logger.LogInformation($"Recieved screenshot from {deviceFound.Environment.MachineName}");
                     deviceFound.ScreenshotFileNames.Add(fileName);
                     deviceFound.IsScreenshotRequired = false;
                 }

                 // Also append to event queue
                 lock (Program.EventQueues)
                 {
                     foreach (var queue in Program.EventQueues)
                     {
                         queue.Events.Enqueue(new EventQueueItem() { DeviceID = deviceFound.ID, EventData = new EventData() { EventDevice = deviceFound }, EventDescription = EventQueueItem.EventKind.DeviceScreenshotRecieved, EventOccured = now });
                         queue.LastEvent = now;
                     }
                 }

                 return Ok(AnswerExtensions.Success(true));
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex.Message);
                 return BadRequest(AnswerExtensions.Fail(ex.Message));
             }*/

            await _homeContext.SaveChangesAsync();
        }
    }
}