using Home.Data;
using Home.Data.Com;
using Home.Data.Events;
using Home.Data.Helper;
using Home.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Home.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(ILogger<DeviceController> logger)
        {
            _logger = logger;
        }


        [HttpPost("register")]
        public IActionResult Register([FromBody] Device device)
        {
            var now = DateTime.Now;

            if (device == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device given"));

            bool result = false;
            lock (Program.Devices)
            {
                if (!Program.Devices.Any(d => d.ID == device.ID))
                {
                    device.Status = Device.DeviceStatus.Active;
                    device.LastSeen = now;
                    device.LogEntries.Clear();
                    device.LogEntries.Add(new LogEntry(now, $"Device {device.Name} was successfully added!", LogEntry.LogLevel.Information));
                    _logger.LogInformation($"New device {device.Environment.MachineName} has just logged in!");
                    device.IsScreenshotRequired = true;
                    Program.Devices.Add(device);

                    lock (Program.EventQueues)
                    {
                        foreach (var queue in Program.EventQueues)
                        {
                            queue.LastEvent = now;
                            queue.Events.Enqueue(new EventQueueItem() { DeviceID = device.ID, EventData = new EventData(device), EventDescription = EventQueueItem.EventKind.NewDeviceConnected, EventOccured = now });
                        }
                    }

                    result = true;
                }
            }

            if (result)
                return Ok(AnswerExtensions.Success(true));

            return BadRequest(AnswerExtensions.Fail("Device-Register couldn't be processed!"));
        }

        [HttpPost("ack")]
        public IActionResult Ack([FromBody] Device refreshedDevice)
        {
            var now = DateTime.Now;

            if (refreshedDevice == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device given"));

            bool result = false;
            bool isScreenshotRequired = false;
            Message hasMessage = null;
            Command hasCommand = null;

            lock (Program.Devices)
            {
                if (Program.Devices.Any(d => d.ID == refreshedDevice.ID))
                {
                    var currentDevice = Program.Devices.Where(p => p.ID == refreshedDevice.ID).FirstOrDefault();
                    if (currentDevice != null)
                    {
                        // Check if device was previously offline
                        if (currentDevice.Status == Device.DeviceStatus.Offline)
                        {
                            // Check for clearing usage stats (if the device was offline for more than one hour)
                            if (currentDevice.LastSeen.AddHours(1) < DateTime.Now)
                                currentDevice.Usage.Clear();

                            currentDevice.LogEntries.Add(new LogEntry(DateTime.Now, $"Device \"{currentDevice.Name}\" has recovered and is now online again!", LogEntry.LogLevel.Information, (refreshedDevice.Type == Device.DeviceType.SingleBoardDevice || refreshedDevice.Type == Device.DeviceType.Server)));
                            currentDevice.IsScreenshotRequired = true;
                        }

                        // Check if a newer client version is used
                        if (currentDevice.ServiceClientVersion != refreshedDevice.ServiceClientVersion && !string.IsNullOrEmpty(currentDevice.ServiceClientVersion))
                            currentDevice.LogEntries.Add(new LogEntry(DateTime.Now, $"Device \"{refreshedDevice.Name}\" detected new client version: {refreshedDevice.ServiceClientVersion}", LogEntry.LogLevel.Information, true));

                        // Detect any device changes and log them (also to Telegram)

                        // CPU
                        if (currentDevice.Environment.CPUName != refreshedDevice.Environment.CPUName && !string.IsNullOrEmpty(refreshedDevice.Environment.CPUName))
                            currentDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected CPU change. CPU {currentDevice.Environment.CPUName} got replaced with {refreshedDevice.Environment.CPUName}", LogEntry.LogLevel.Information, true));
                        if (currentDevice.Environment.CPUCount != refreshedDevice.Environment.CPUCount && refreshedDevice.Environment.CPUCount > 0)
                            currentDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected CPU-Count change from {currentDevice.Environment.CPUCount} to {refreshedDevice.Environment.CPUCount}", LogEntry.LogLevel.Information, true));

                        // OS (Ignore Windows Updates, just document enum chnages)
                        if (currentDevice.OS != refreshedDevice.OS)
                            currentDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected OS change from {currentDevice.OS} to {refreshedDevice.OS}", LogEntry.LogLevel.Information, true));

                        // Motherboard
                        if (currentDevice.Environment.Motherboard != refreshedDevice.Environment.Motherboard && !string.IsNullOrEmpty(refreshedDevice.Environment.Motherboard))
                            currentDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected Motherboard change from {currentDevice.Environment.Motherboard} to {refreshedDevice.Environment.Motherboard}", LogEntry.LogLevel.Information, true));

                        // Graphics
                        //if (oldDevice.Envoirnment.Graphics != refreshedDevice.Envoirnment.Graphics && !string.IsNullOrEmpty(refreshedDevice.Envoirnment.Graphics))
                        //    oldDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected Graphics change from {oldDevice.Envoirnment.Graphics} to {refreshedDevice.Envoirnment.Graphics}", LogEntry.LogLevel.Information, true));
                        if (currentDevice.Environment.GraphicCards.Count != refreshedDevice.Environment.GraphicCards.Count)
                        {
                            if (refreshedDevice.Environment.GraphicCards.Count == 0 && !string.IsNullOrEmpty(refreshedDevice.Environment.Graphics))
                            {
                                // ignore
                            }
                            else
                            {
                                foreach (var item in refreshedDevice.Environment.GraphicCards)
                                    currentDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected Graphics change(s) ({item})", LogEntry.LogLevel.Information, true));
                            }
                        }

                        // RAM
                        if (currentDevice.Environment.TotalRAM != refreshedDevice.Environment.TotalRAM && refreshedDevice.Environment.TotalRAM > 0)
                            currentDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected RAM change from {currentDevice.Environment.TotalRAM} GB to {refreshedDevice.Environment.TotalRAM} GB", LogEntry.LogLevel.Information, true));

                        // IP Change
                        if (currentDevice.IP.Replace("/24", string.Empty) != refreshedDevice.IP.Replace("/24", string.Empty) && !string.IsNullOrEmpty(refreshedDevice.IP))
                            currentDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected IP change from {currentDevice.IP} to {refreshedDevice.IP}", LogEntry.LogLevel.Information, true));

                        isScreenshotRequired = currentDevice.IsScreenshotRequired;

                        // If this device is live, ALWAYS send a screenshot on ack!
                        if (currentDevice.IsLive.HasValue && currentDevice.IsLive.Value)
                            isScreenshotRequired = true;

                        // USAGE
                        // CPU & DISK
                        currentDevice.Usage.AddCPUEntry(refreshedDevice.Environment.CPUUsage);
                        currentDevice.Usage.AddDISKEntry(refreshedDevice.Environment.DiskUsage);

                        // RAM
                        var ram = refreshedDevice.Environment.FreeRAM.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                        if (ram != null && double.TryParse(ram, out double res))
                            currentDevice.Usage.AddRAMEntry(res);

                        // Update device
                        currentDevice.Update(refreshedDevice, now, Device.DeviceStatus.Active);                        

                        if (currentDevice.DiskDrives.Count > 0)
                        {
                            var dds = currentDevice.DiskDrives.Where(d => d.IsFull().HasValue && d.IsFull().Value).ToList();
                            
                            // Add storage warning
                            // But ensure that the warning is only once per device and will be added again if dismissed by the user
                            if (dds.Count > 0)
                            {
                                foreach (var disk in dds)
                                {
                                    // Check for already existing storage warnings       
                                    if (currentDevice.StorageWarnings.Any(s => s.StorageID == disk.UniqueID))
                                        continue;

                                    // Add storage warning
                                    var warning = StorageWarning.Create($"DISK: {disk} is low on storage. Free space left: {ByteUnit.FindUnit(disk.FreeSpace)}");
                                    currentDevice.StorageWarnings.Add(warning);
                                    currentDevice.LogEntries.Add(warning.ConvertToLogEntry());
                                }
                            }
                        }

                        lock (currentDevice.Messages)
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
                        }
                        
                        result = true;

                        lock (Program.EventQueues)
                        {
                            foreach (var queue in Program.EventQueues)
                            {
                                queue.LastEvent = now;
                                queue.Events.Enqueue(new EventQueueItem() { DeviceID = refreshedDevice.ID, EventData = new EventData() { EventDevice = currentDevice }, EventDescription = EventQueueItem.EventKind.ACK, EventOccured = now });
                            }
                        }
                    }
                }  
                else
                {
                    // Temporay fix if data is empty again :(
                    Program.Devices.Add(refreshedDevice);

                    lock (Program.EventQueues)
                    {
                        foreach (var queue in Program.EventQueues)
                        {
                            queue.LastEvent = now;
                            queue.Events.Enqueue(new EventQueueItem() { DeviceID = refreshedDevice.ID, EventData = new EventData() { EventDevice = refreshedDevice }, EventDescription = EventQueueItem.EventKind.NewDeviceConnected, EventOccured = now });
                        }
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
                return Ok(AnswerExtensions.Success(ackResult));
            }

            return BadRequest(new Answer<AckResult>("fail", new AckResult(AckResult.Ack.Invalid)) { ErrorMessage = "Device-ACK couldn't be processed. This device was not logged in before!" });
        }

        [HttpPost("screenshot")]
        public async Task<IActionResult> PostScreenshot([FromBody] Screenshot shot)
        {
            var now = DateTime.Now;
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
                    string clientPath = System.IO.Path.Combine(Program.SCREENSHOTS_PATH, deviceFound.ID);

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
            }
        }
    }
}