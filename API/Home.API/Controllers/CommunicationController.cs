using Home.Data;
using Home.Data.Com;
using Home.Data.Events;
using Home.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Home.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CommunicationController : ControllerBase
    {
        private readonly ILogger<CommunicationController> _logger;

        public CommunicationController(ILogger<CommunicationController> logger)
        {
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] Client client)
        {
            if (client == null || !client.IsRealClient)
                return NotFound(AnswerExtensions.Fail("Invalid client data!"));

            if (!Program.Clients.Any(p => p.ID == client.ID))
            {
                lock (Program.Clients)
                {
                    Program.Clients.Add(client);
                }

                lock (Program.EventQueues)
                {
                    Program.EventQueues.Add(new Data.Events.EventQueue() { ClientID = client.ID, LastClientRequest = DateTime.Now });
                }

                _logger.LogInformation($"Client {client} has just logged in!");
            }
            else
            {
                _logger.LogWarning($"Client {client} was already logged in!");

                // But updated LastClientRequest
                lock (Program.EventQueues)
                {
                    var queue = Program.EventQueues.Where(d => d.ClientID == client.ID).FirstOrDefault();
                    if (queue != null)
                        queue.LastClientRequest = DateTime.Now;
                }

                // return BadRequest(AnswerExtensions.Fail("Already logged in"));
            }

            return Ok(AnswerExtensions.Success(Program.Devices));
        }

        [HttpPost("logoff")]
        public IActionResult LogOff([FromBody] Client client)
        {
            if (client == null || !client.IsRealClient)
                return NotFound(AnswerExtensions.Fail("Invalid client data"));

            if (Program.Clients.Any(p => p.ID == client.ID))
            {
                lock (Program.Clients)
                {
                    Client cl = Program.Clients.Where(p => p.ID == client.ID).FirstOrDefault();
                    if (cl != null)
                        Program.Clients.Remove(cl);


                    lock (Program.EventQueues)
                    {
                        EventQueue eventQueue = Program.EventQueues.Where(p => p.ClientID == client.ID).FirstOrDefault();
                        if (eventQueue != null)
                            Program.EventQueues.Remove(eventQueue);
                    }

                    // Clean UP liveMode Assoc
                    if (Program.LiveModeAssoc.ContainsKey(cl))
                    {
                        var devices = Program.LiveModeAssoc[cl].Select(d => Program.Devices.FirstOrDefault(f => f.ID == d));

                        // Check if we can set the device to false (if true),
                        // because if this device is also used by another client, we cannot set it to false then.
                        foreach (var partictularDevice in devices)
                        {
                            bool found = false;

                            // Check if we can disable this device (if it is not used by any other clients)
                            foreach (var item in Program.LiveModeAssoc.Keys)
                            {
                                if (item.ID == cl.ID)
                                    continue;

                                if (Program.LiveModeAssoc[item].Contains(partictularDevice.ID))
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                lock (Program.Devices)
                                {
                                    partictularDevice.IsLive = false;
                                    partictularDevice.LogEntries.Add(new LogEntry($"Device \"{partictularDevice.Name}\" status changed to normal, because one or multiple clients (those that have aquired live view) have logged off!", LogEntry.LogLevel.Information, false));
                                }
                                
                            }
                        }

                        Program.LiveModeAssoc.Remove(cl);
                    }

                }

                _logger.LogInformation($"Client {client} has just logged off!");
            }
            else
                return BadRequest(AnswerExtensions.Fail("Already logged off"));

            return Ok(AnswerExtensions.Success("ok"));
        }

        [HttpPost("update")]
        public IActionResult Update([FromBody] Client client)
        {
            if (client == null || !client.IsRealClient)
                return NotFound(AnswerExtensions.Fail("Invalid client data"));

            lock (Program.EventQueues)
            {
                var queue = Program.EventQueues.Where(p => p.ClientID == client.ID).FirstOrDefault();
                if (queue != null)
                {
                    queue.LastClientRequest = DateTime.Now;

                    if (queue.Events.Count > 0)
                        return Ok(AnswerExtensions.Success(queue.Events.Dequeue()));
                    else
                        return BadRequest(new Answer<EventQueueItem>("ok", null) { ErrorMessage = "Queue is currently empty ..." });
                }
            }

            return NotFound(AnswerExtensions.Fail("Client not found!"));
        }

        [HttpGet("get_screenshot/{clientId}/{deviceId}")]
        public IActionResult AskForScreenshot(string clientId, string deviceID)
        {
            if (string.IsNullOrEmpty(clientId))
                return BadRequest(AnswerExtensions.Fail("Invalid client data"));

            if (string.IsNullOrEmpty(deviceID))
                return BadRequest(AnswerExtensions.Fail("Invalid device data"));

            Client cl = null;
            lock (Program.Clients)
            {
                if (!Program.Clients.Any(p => p.ID == clientId))
                    return NotFound(AnswerExtensions.Fail("Client not found!"));
                else
                    cl = Program.Clients.Where(c => c.ID == clientId).FirstOrDefault();
            }

            lock (Program.Devices)
            {
                if (!Program.Devices.Any(p => p.ID == deviceID))
                    return NotFound(AnswerExtensions.Fail("Device not found!"));

                var device = Program.Devices.Where(p => p.ID == deviceID).FirstOrDefault();
                if (device != null)
                {
                    if (device.OS == Device.OSType.Android)
                        return BadRequest(AnswerExtensions.Fail("Android Device doesn't support screenshots!"));

                    device.IsScreenshotRequired = true;
                    _logger.LogInformation($"Aquired screenshot from {cl?.Name} for device {device.Name}!");
                }
            }

            return Ok(AnswerExtensions.Success(true));
        }

        [HttpGet("recieve_screenshot/{deviceId}/{fileName}")]
        public IActionResult RecieveScreenshot(string deviceId, string fileName)
        {
            if (string.IsNullOrEmpty(deviceId))
                return BadRequest(AnswerExtensions.Fail("Invalid device data"));

            try
            {
                string screenshotFilePath = System.IO.Path.Combine(Program.SCREENSHOTS_PATH, deviceId, $"{fileName}.png");
                Screenshot screenshot = new Screenshot()
                {
                    ClientID = null,
                    Data = Convert.ToBase64String(System.IO.File.ReadAllBytes(screenshotFilePath))
                };

                return Ok(AnswerExtensions.Success(screenshot));
            }
            catch (Exception ex)
            {
                return BadRequest(AnswerExtensions.Fail(ex.Message));
            }
        }

        [HttpGet("clear_log/{deviceID}")]
        public IActionResult ClearDeviceLog(string deviceID)
        {
            if (string.IsNullOrEmpty(deviceID))
                return BadRequest(AnswerExtensions.Fail("Invalid device data"));

            lock (Program.Devices)
            {
                if (!Program.Devices.Any(p => p.ID == deviceID))
                    return NotFound(AnswerExtensions.Fail("Device not found!"));

                var device = Program.Devices.Where(p => p.ID == deviceID).FirstOrDefault();
                if (device != null)
                {
                    device.LogEntries.Clear();
                    _logger.LogInformation($"Clearing log of {device.Name} ...");
                    lock (Program.EventQueues)
                    {
                        foreach (var queue in Program.EventQueues)
                        {
                            queue.LastEvent = DateTime.Now;
                            queue.Events.Enqueue(new EventQueueItem() { DeviceID = deviceID, EventData = new EventData(device), EventDescription = EventQueueItem.EventKind.LogCleared, EventOccured = DateTime.Now });
                        }
                    }
                }
            }

            return Ok(AnswerExtensions.Success("ok"));
        }

        [HttpPost("send_message")]
        public IActionResult SendMessage([FromBody] Message message)
        {
            if (message == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device data!"));

            Device device = null;
            // Check if this devices exists
            lock (Program.Devices)
            {
                if (!Program.Devices.Any(p => p.ID == message.DeviceID))
                    return BadRequest(AnswerExtensions.Fail("Device doesn't exists!"));
                else
                    device = Program.Devices.Where(p => p.ID == message.DeviceID).FirstOrDefault();
            }

            _logger.LogInformation($"Sent message to {device.Name}: {message}");
            lock (device.Messages)
            {
                device.Messages.Enqueue(message);
            }

            LogEntry.LogLevel level = LogEntry.LogLevel.Information;
            switch (message.Type)
            {
                case Message.MessageImage.Information: level = LogEntry.LogLevel.Information; break;
                case Message.MessageImage.Warning: level = LogEntry.LogLevel.Warning; break;
                case Message.MessageImage.Error: level = LogEntry.LogLevel.Error; break;
            }

            device.LogEntries.Add(new LogEntry(DateTime.Now, $"Recieved message: {message}", level));

            lock (Program.EventQueues)
            {
                foreach (var queue in Program.EventQueues)
                {
                    queue.LastEvent = DateTime.Now;
                    queue.Events.Enqueue(new EventQueueItem() { DeviceID = device.ID, EventData = new EventData(device), EventDescription = EventQueueItem.EventKind.LogEntriesRecieved, EventOccured = DateTime.Now });
                }
            }

            return Ok(AnswerExtensions.Success("ok"));
        }

        [HttpGet("status/{clientId}/{deviceId}/{live:bool}")]
        public IActionResult SetLiveStatus(string clientId, string deviceId, bool live)
        {
            lock (Program.Devices)
            {
                var device = Program.Devices.FirstOrDefault(d => d.ID == deviceId);
                if (device == null)
                    return BadRequest(AnswerExtensions.Fail($"Device couldn't be found: {deviceId}"));

                if (device.Status == Device.DeviceStatus.Offline)
                    return BadRequest(AnswerExtensions.Fail("Cannot set live status if device is offline!"));

                lock (Program.Clients)
                {
                    var client = Program.Clients.FirstOrDefault(c => c.ID == clientId);
                    if (client == null)
                        return BadRequest(AnswerExtensions.Fail($"Client couldn't be found: {clientId}"));

                    if (Program.LiveModeAssoc.ContainsKey(client))
                    {
                        var list = Program.LiveModeAssoc[client];
                        if (!list.Contains(deviceId))
                            list.Add(deviceId);
                    }
                    else
                        Program.LiveModeAssoc.Add(client, new List<string>() { deviceId });

                    lock (Program.Devices)
                    {
                        device.IsLive = live;
                        device.LogEntries.Add(new LogEntry($"Device \"{device.Name}\" status changed to {(live ? "live" : "normal")} by client {client.Name}!", LogEntry.LogLevel.Information, false));
                    }
                }
                return Ok(AnswerExtensions.Success("ok"));
            }
        }

        [HttpPost("send_command")]
        public IActionResult SendCommnad([FromBody] Command command)
        {
            if (command == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device data!"));

            Device device = null;
            // Check if this devices exists
            lock (Program.Devices)
            {
                if (!Program.Devices.Any(p => p.ID == command.DeviceID))
                    return BadRequest(AnswerExtensions.Fail("Device doesn't exists!"));
                else
                    device = Program.Devices.Where(p => p.ID == command.DeviceID).FirstOrDefault();
            }

            _logger.LogInformation($"Sent command to {device.Name}: {command}");
            lock (device.Commands)
                device.Commands.Enqueue(command);

            device.LogEntries.Add(new LogEntry(DateTime.Now, $"Recieved command: {command}", LogEntry.LogLevel.Information));

            lock (Program.EventQueues)
            {
                foreach (var queue in Program.EventQueues)
                {
                    queue.LastEvent = DateTime.Now;
                    queue.Events.Enqueue(new EventQueueItem() { DeviceID = device.ID, EventData = new EventData(device), EventDescription = EventQueueItem.EventKind.LogEntriesRecieved, EventOccured = DateTime.Now });
                }
            }

            return Ok(AnswerExtensions.Success("ok"));
        }
    }
}