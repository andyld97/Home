﻿using Home.Data;
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
        public IActionResult Ack([FromBody] Device device)
        {
            var now = DateTime.Now;

            if (device == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device given"));

            bool result = false;
            bool isScreenshotRequired = false;
            Message hasMessage = null;

            lock (Program.Devices)
            {
                if (Program.Devices.Any(d => d.ID == device.ID))
                {
                    var dev = Program.Devices.Where(p => p.ID == device.ID).FirstOrDefault();
                    if (dev != null)
                    {
                        // Too many log entries (only report important log entries) and ack works fine now!
                        // device.LogEntries.Add("Recieved ack!".FormatLogLine(now));
                        // _logger.LogInformation($"Recieved ack from device {device}!");
                        if (dev.Status == Device.DeviceStatus.Offline)
                        {
                            dev.LogEntries.Add(new LogEntry(DateTime.Now, "Device has recovered and is now online again!", LogEntry.LogLevel.Information));
                            dev.IsScreenshotRequired = true;
                        }
                        if (dev.ServiceClientVersion != device.ServiceClientVersion && !string.IsNullOrEmpty(dev.ServiceClientVersion))
                            dev.LogEntries.Add(new LogEntry(DateTime.Now, $"Detected new client version: {device.ServiceClientVersion}", LogEntry.LogLevel.Information));

                        isScreenshotRequired = dev.IsScreenshotRequired;
                        dev.Update(device, now, Device.DeviceStatus.Active);
                        // dev.IsScreenshotRequired = false;
                        // Will not be set here (only will be set if a screenshot was posted)

                        lock (dev.Messages)
                        {
                            if (dev.Messages.Count != 0)
                                hasMessage = dev.Messages.Dequeue();
                        }
                        
                        result = true;

                        lock (Program.EventQueues)
                        {
                            foreach (var queue in Program.EventQueues)
                            {
                                queue.LastEvent = now;
                                queue.Events.Enqueue(new EventQueueItem() { DeviceID = device.ID, EventData = new EventData() { EventDevice = dev }, EventDescription = EventQueueItem.EventKind.ACK, EventOccured = now });
                            }
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