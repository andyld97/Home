﻿using Home.API.Helper;
using Home.API.home;
using Home.API.home.Models;
using Home.Data;
using Home.Data.Com;
using Home.Data.Events;
using Home.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Device = Home.API.home.Models.Device;

namespace Home.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CommunicationController : ControllerBase
    {
        private readonly ILogger<CommunicationController> _logger;
        private readonly HomeContext _context;

        public CommunicationController(ILogger<CommunicationController> logger, HomeContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Performs a login for the given client
        /// </summary>
        /// <param name="client">Home.WPF App</param>
        /// <returns>Ok on success</returns>
        [HttpPost("login")]
        public async Task<IActionResult> LoginClientAsync([FromBody] Client client)
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

            var devices = await _context.GetAllDevicesAsync(true);
            List<Home.Model.Device> result = new List<Home.Model.Device>();
            foreach (var item in devices.OrderBy(p => p.Name))
                result.Add(ModelConverter.ConvertDevice(item));

            return Ok(AnswerExtensions.Success(result));
        }

        /// <summary>
        /// Performs a logoff for the given client
        /// </summary>
        /// <param name="client">Home.WPF App</param>
        /// <returns>Ok on success</returns>
        [HttpPost("logoff")]
        public async Task<IActionResult> LogOffClientAsync([FromBody] Client client)
        {
            if (client == null || !client.IsRealClient)
                return NotFound(AnswerExtensions.Fail("Invalid client data"));

            if (Program.Clients.Any(p => p.ID == client.ID))
            {
                Client cl;
                lock (Program.Clients)
                {
                    cl = Program.Clients.Where(p => p.ID == client.ID).FirstOrDefault();
                    if (cl != null)
                        Program.Clients.Remove(cl);

                    lock (Program.EventQueues)
                    {
                        EventQueue eventQueue = Program.EventQueues.Where(p => p.ClientID == client.ID).FirstOrDefault();
                        if (eventQueue != null)
                            Program.EventQueues.Remove(eventQueue);
                    }
                }

                // Clean UP liveMode Assoc
                if (Program.LiveModeAssoc.ContainsKey(cl))
                {
                    var allDevices = await _context.Device.Include(p => p.DeviceLog).ToListAsync();
                    var devices = Program.LiveModeAssoc[cl].Select(d => allDevices.FirstOrDefault(f => f.Guid == d));

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

                            if (Program.LiveModeAssoc[item].Contains(partictularDevice.Guid))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.LiveModeChanged, partictularDevice);
                            partictularDevice.IsLive = false;
                            var logEntry = ModelConverter.CreateLogEntry(partictularDevice, $"Device \"{partictularDevice.Name}\" status changed to normal, because one or multiple clients (those that have aquired live view) have logged off!", LogEntry.LogLevel.Information, false);
                            await _context.DeviceLog.AddAsync(logEntry);
                        }
                    }

                    await _context.SaveChangesAsync();
                    Program.LiveModeAssoc.Remove(cl);
                }                

                _logger.LogInformation($"Client {client} has just logged off!");
            }
            else
                return BadRequest(AnswerExtensions.Fail("Already logged off"));

            return Ok(AnswerExtensions.Success("ok"));
        }

        /// <summary>
        /// Updates the client using the associated event queue
        /// </summary>
        /// <param name="client">Home.WPF App</param>
        /// <returns>Ok on success</returns>
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

        /// <summary>
        /// Request a screenshot from the given device
        /// </summary>
        /// <param name="clientId">Home.WPF App</param>
        /// <param name="deviceID">Selected device</param>
        /// <returns>Ok on success</returns>
        [HttpGet("get_screenshot/{clientId}/{deviceId}")]
        public async Task<IActionResult> AskForScreenshotAsync(string clientId, string deviceID)
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

            var device = await _context.Device.Where(p => p.Guid == deviceID).FirstOrDefaultAsync();
            if (device == null)
                return NotFound(AnswerExtensions.Fail("Device not found!"));

            if (device.Ostype == (int)Home.Model.Device.OSType.Android)
                return BadRequest(AnswerExtensions.Fail("Android Device doesn't support screenshots!"));

            device.IsScreenshotRequired = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Aquired screenshot from {cl?.Name} for device {device.Name}!");

            return Ok(AnswerExtensions.Success(true));
        }

        /// <summary>
        /// Gets a screenshot for the given device
        /// </summary>
        /// <param name="deviceId">The selected device</param>
        /// <param name="fileName">The screenshot filename</param>
        /// <returns>Ok on success</returns>
        [HttpGet("recieve_screenshot/{deviceId}/{fileName}")]
        public IActionResult RecieveScreenshot(string deviceId, string fileName)
        {
            if (string.IsNullOrEmpty(deviceId))
                return BadRequest(AnswerExtensions.Fail("Invalid device data"));

            try
            {
                string screenshotFilePath = System.IO.Path.Combine(Config.SCREENSHOTS_PATH, deviceId, $"{fileName}.png");
                Screenshot screenshot = new Screenshot()
                {
                    DeviceID = null,
                    Data = Convert.ToBase64String(System.IO.File.ReadAllBytes(screenshotFilePath))
                };

                return Ok(AnswerExtensions.Success(screenshot));
            }
            catch (Exception ex)
            {
                return BadRequest(AnswerExtensions.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Clears the device log of the given device completely
        /// </summary>
        /// <param name="deviceID">The selected device</param>
        /// <returns>Ok on success</returns>
        [HttpGet("clear_log/{deviceID}")]
        public async Task<IActionResult> ClearDeviceLogAsync(string deviceID)
        {
            if (string.IsNullOrEmpty(deviceID))
                return BadRequest(AnswerExtensions.Fail("Invalid device data"));

            var device = await _context.Device.Include(d => d.DeviceLog).Where(p => p.Guid == deviceID).FirstOrDefaultAsync();
            if (device != null)
            {
                device.DeviceLog.Clear();
                
                _logger.LogInformation($"Clearing log of {device.Name} ...");
                ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.LogCleared, device);
                return Ok(AnswerExtensions.Success("ok"));
            }
            else
                return NotFound(AnswerExtensions.Fail("Device not found!"));
        }

        /// <summary>
        /// Sends a message to the device
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>Ok on success</returns>
        [HttpPost("send_message")]
        public async Task<IActionResult> SendMessageAsync([FromBody] Message message)
        {
            if (message == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device data!"));

            var device = await _context.GetDeviceByIdAsync(message.DeviceID);
            if (device == null)
                return BadRequest(AnswerExtensions.Fail("Device doesn't exists!"));                

            _logger.LogInformation($"Sent message to {device.Name}: {message}");
            device.DeviceMessage.Add(new home.Models.DeviceMessage() { Content = message.Content, Title = message.Title, Type = (short)message.Type, Timestamp = DateTime.Now, IsRecieved = false });
            
            LogEntry.LogLevel level = LogEntry.LogLevel.Information;
            switch (message.Type)
            {
                case Message.MessageImage.Information: level = LogEntry.LogLevel.Information; break;
                case Message.MessageImage.Warning: level = LogEntry.LogLevel.Warning; break;
                case Message.MessageImage.Error: level = LogEntry.LogLevel.Error; break;
            }

            await _context.DeviceLog.AddAsync(ModelConverter.CreateLogEntry(device, $"Recieved message: {message}", level, false));
            await _context.SaveChangesAsync();

            ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, device);
            return Ok(AnswerExtensions.Success("ok"));
        }

        /// <summary>
        /// Set the live status of the given device<br/>
        /// "Live" means that the device is asked for a screenshot on every ack
        /// </summary>
        /// <param name="clientId">Home.WPF App</param>
        /// <param name="deviceId">The selected device</param>
        /// <param name="live">Enable or Disable live status</param>
        /// <returns>Ok on success</returns>
        [HttpGet("status/{clientId}/{deviceId}/{live:bool}")]
        public async Task<IActionResult> SetLiveStatusAsync(string clientId, string deviceId, bool live)
        {
            var device = await _context.Device.Include(d => d.DeviceLog).Where(p => p.Guid == deviceId).FirstOrDefaultAsync();
            if (device == null)
                return BadRequest(AnswerExtensions.Fail($"Device couldn't be found: {deviceId}"));

            if (!device.Status) // == Device.DeviceStatus.Offline)
                return BadRequest(AnswerExtensions.Fail("Cannot set live status if device is offline!"));

            Client client;
            lock (Program.Clients)
            {
                client = Program.Clients.FirstOrDefault(c => c.ID == clientId);
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
            }

            device.IsLive = live;
            ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.LiveModeChanged, device);
            var logEntry = ModelConverter.CreateLogEntry(device, $"Device \"{device.Name}\" status changed to {(live ? "live" : "normal")} by client {client.Name}!", LogEntry.LogLevel.Information, false);
            await _context.DeviceLog.AddAsync(logEntry);

            return Ok(AnswerExtensions.Success("ok"));
        }

        /// <summary>
        /// Deletes a device from the database completly
        /// </summary>
        /// <param name="guid">The id of the device</param>
        /// <returns>Ok on success</returns>
        [HttpDelete("delete/{guid}")]
        public async Task<IActionResult> DeleteDeviceAsync(string guid)
        {
            var device = await _context.Device.Where(p => p.Guid == guid).FirstOrDefaultAsync();

            if (device != null)
            {
                _context.Device.Remove(device);
                
                // ToDo: *** Remove all other related entries from other tables
                await _context.SaveChangesAsync();
                return Ok(AnswerExtensions.Success("ok"));
            }
            else
                return BadRequest(AnswerExtensions.Fail("This device doesn't exists!"));
        }

        /// <summary>
        /// Sends a command to the given device
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>Ok on succeess</returns>
        [HttpPost("send_command")]
        public async Task<IActionResult> SendCommnadAsync([FromBody] Command command)
        {
            if (command == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device data!"));

            // Check if this devices exists
            home.Models.Device device = await _context.GetDeviceByIdAsync(command.DeviceID); // Include(d => d.DeviceCommand).Include(d => d.DeviceLog).Where(p => p.Guid == command.DeviceID).FirstOrDefaultAsync();

            if (device == null)
                return BadRequest(AnswerExtensions.Fail("Device doesn't exists!"));

            _logger.LogInformation($"Sent command to {device.Name}: {command}");
            device.DeviceCommand.Add(new home.Models.DeviceCommand() { Executable = command.Executable, IsExceuted = false, Parameter = command.Parameter, Timestamp = DateTime.Now });
            await _context.DeviceLog.AddAsync(ModelConverter.CreateLogEntry(device, $"Recieved command: {command}", LogEntry.LogLevel.Information, false));

            ClientHelper.NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, device);
            
            await _context.SaveChangesAsync();
            return Ok(AnswerExtensions.Success("ok"));
        }

        /// <summary>
        /// A connection test
        /// </summary>
        /// <returns>Ok on success</returns>
        [HttpGet("test")]
        public IActionResult ConnectionTest()
        {
            // Also ensure that the db connection is working (test)
            var dummy = _context.Device.Where(d => d.Name == "test").FirstOrDefault();  
            return Ok();
        }
    }
}