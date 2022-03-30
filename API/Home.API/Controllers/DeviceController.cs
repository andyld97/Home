using Home.Data;
using Home.Data.Com;
using Home.Data.Events;
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
                    _logger.LogInformation($"New device {device.Envoirnment.MachineName} has just logged in!");
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
                    var oldDevice = Program.Devices.Where(p => p.ID == refreshedDevice.ID).FirstOrDefault();
                    if (oldDevice != null)
                    {
                        // Check if device was previously offline
                        if (oldDevice.Status == Device.DeviceStatus.Offline)
                        {
                            // Check for clearing usage stats (if the device was offline for more than one hour)
                            if (oldDevice.LastSeen.AddHours(1) < DateTime.Now)
                                oldDevice.Usage.Clear();

                            oldDevice.LogEntries.Add(new LogEntry(DateTime.Now, $"Device \"{oldDevice.Name}\" has recovered and is now online again!", LogEntry.LogLevel.Information, (refreshedDevice.Type == Device.DeviceType.SingleBoardDevice || refreshedDevice.Type == Device.DeviceType.Server)));
                            oldDevice.IsScreenshotRequired = true;
                        }

                        // Check if a newer client version is used
                        if (oldDevice.ServiceClientVersion != refreshedDevice.ServiceClientVersion && !string.IsNullOrEmpty(oldDevice.ServiceClientVersion))
                            oldDevice.LogEntries.Add(new LogEntry(DateTime.Now, $"Detected new client version: {refreshedDevice.ServiceClientVersion}", LogEntry.LogLevel.Information));

                        // Detect any device changes and log them (also to Telegram)

                        // CPU
                        if (oldDevice.Envoirnment.CPUName != refreshedDevice.Envoirnment.CPUName && !string.IsNullOrEmpty(refreshedDevice.Envoirnment.CPUName))
                            oldDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected CPU change. CPU {oldDevice.Envoirnment.CPUName} got replaced with {refreshedDevice.Envoirnment.CPUName}", LogEntry.LogLevel.Information, true));
                        if (oldDevice.Envoirnment.CPUCount != refreshedDevice.Envoirnment.CPUCount && refreshedDevice.Envoirnment.CPUCount > 0)
                            oldDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected CPU-Count change from {oldDevice.Envoirnment.CPUCount} to {refreshedDevice.Envoirnment.CPUCount}", LogEntry.LogLevel.Information, true));

                        // OS (Ignore Windows Updates, just document enum chnages)
                        if (oldDevice.OS != refreshedDevice.OS)
                            oldDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected OS change from {oldDevice.OS} to {refreshedDevice.OS}", LogEntry.LogLevel.Information, true));

                        // Motherboard
                        if (oldDevice.Envoirnment.Motherboard != refreshedDevice.Envoirnment.Motherboard && !string.IsNullOrEmpty(refreshedDevice.Envoirnment.Motherboard))
                            oldDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected Motherboard change from {oldDevice.Envoirnment.Motherboard} to {refreshedDevice.Envoirnment.Motherboard}", LogEntry.LogLevel.Information, true));

                        // Graphics
                        if (oldDevice.Envoirnment.Graphics != refreshedDevice.Envoirnment.Graphics && !string.IsNullOrEmpty(refreshedDevice.Envoirnment.Graphics))
                            oldDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected Motherboard change from {oldDevice.Envoirnment.Graphics} to {refreshedDevice.Envoirnment.Graphics}", LogEntry.LogLevel.Information, true));

                        // RAM
                        if (oldDevice.Envoirnment.TotalRAM != refreshedDevice.Envoirnment.TotalRAM && refreshedDevice.Envoirnment.TotalRAM > 0)
                            oldDevice.LogEntries.Add(new LogEntry($"Device \"{refreshedDevice.Name}\" detected RAM change from {oldDevice.Envoirnment.TotalRAM} GB to {refreshedDevice.Envoirnment.TotalRAM} GB", LogEntry.LogLevel.Information, true));

                        isScreenshotRequired = oldDevice.IsScreenshotRequired;

                        // If this device is live, ALWAYS send a screenshot on ack!
                        if (oldDevice.IsLive.HasValue && oldDevice.IsLive.Value)
                            isScreenshotRequired = true;

                        // USAGE
                        // CPU & DISK
                        oldDevice.Usage.AddCPUEntry(refreshedDevice.Envoirnment.CPUUsage);
                        oldDevice.Usage.AddDISKEntry(refreshedDevice.Envoirnment.DiskUsage);

                        // RAM
                        var ram = refreshedDevice.Envoirnment.FreeRAM.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                        if (ram != null && double.TryParse(ram, out double res))
                            oldDevice.Usage.AddRAMEntry(res);

                        // Update device
                        oldDevice.Update(refreshedDevice, now, Device.DeviceStatus.Active);

                        lock (oldDevice.Messages)
                        {
                            if (oldDevice.Messages.Count != 0)
                                hasMessage = oldDevice.Messages.Dequeue();
                        }

                        if (hasMessage == null)
                        {
                            lock (oldDevice.Commands)
                            {
                                // Check for commands
                                if (oldDevice.Commands.Count != 0)
                                    hasCommand = oldDevice.Commands.Dequeue();
                            }
                        }
                        
                        result = true;

                        lock (Program.EventQueues)
                        {
                            foreach (var queue in Program.EventQueues)
                            {
                                queue.LastEvent = now;
                                queue.Events.Enqueue(new EventQueueItem() { DeviceID = refreshedDevice.ID, EventData = new EventData() { EventDevice = oldDevice }, EventDescription = EventQueueItem.EventKind.ACK, EventOccured = now });
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
                    _logger.LogInformation($"Recieved screenshot from {deviceFound.Envoirnment.MachineName}");
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