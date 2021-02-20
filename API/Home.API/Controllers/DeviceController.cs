using Home.Data;
using Home.Data.Events;
using Home.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using static Home.Data.Helper.GeneralHelper;

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
                    device.LogEntries.Add($"Device {device.Name} was successfully added!".FormatLogLine(now));
                    _logger.LogInformation($"New device {device} has just logged in!");
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
            lock (Program.Devices)
            {
                if (Program.Devices.Any(d => d.ID == device.ID))
                {
                    var dev = Program.Devices.Where(p => p.ID == device.ID).FirstOrDefault();
                    if (dev != null)
                    {
                        device.LogEntries.Add("Recieved ack!".FormatLogLine(now));
                        _logger.LogInformation($"Recieved ack from device {device}!");
                        dev.Update(device, now, Device.DeviceStatus.Active);
                        
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
                return Ok(AnswerExtensions.Success(true));

            return BadRequest(AnswerExtensions.Fail("Device-ACK couldn't be processed. This device was not logged in before!"));
        }

    }
}
