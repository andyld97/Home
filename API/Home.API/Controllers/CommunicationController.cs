﻿using Home.API.Helper;
using Home.API.home;
using Home.API.home.Models;
using Home.API.Services;
using Home.Data;
using Home.Data.Com;
using Home.Data.Events;
using Home.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Device = Home.API.home.Models.Device;
using static Home.Data.Helper.GeneralHelper;

namespace Home.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CommunicationController : ControllerBase
    {
        private readonly ILogger<CommunicationController> _logger;
        private IClientService _clientService;
        private readonly HomeContext _context;
        private readonly WebhookAPI.Webhook _webhookService;

        public CommunicationController(ILogger<CommunicationController> logger, IClientService clientService, HomeContext context, WebhookAPI.Webhook webhookService)
        {
            _logger = logger;
            _clientService = clientService;
            _context = context;
            _webhookService = webhookService;
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

                // But update LastClientRequest anyways
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
                    foreach (var particularDevice in devices)
                    {
                        bool found = false;

                        // Check if we can disable this device (if it is not used by any other clients)
                        foreach (var item in Program.LiveModeAssoc.Keys)
                        {
                            if (item.ID == cl.ID)
                                continue;

                            if (Program.LiveModeAssoc[item].Contains(particularDevice.Guid))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            // Fully materialize device here
                            _clientService.NotifyClientQueues(EventQueueItem.EventKind.LiveModeChanged, (await _context.GetDeviceByIdAsync(particularDevice.Guid)));
                            particularDevice.IsLive = false;
                            var logEntry = ModelConverter.CreateLogEntry(particularDevice, $"Device \"{particularDevice.Name}\" status changed to normal, because one or multiple clients (those that have acquired live view) have logged off!", LogEntry.LogLevel.Information, false);
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

                    List<EventQueueItem> items = new List<EventQueueItem>();
                    items.AddRange(queue.LastAck);
                    queue.LastAck.Clear();

                    if (queue.Events.Count > 0)
                        items.Add(queue.Events.Dequeue());

                    if (items.Count > 0)
                        return Ok(AnswerExtensions.Success(items));
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
            _logger.LogInformation($"Acquired screenshot from {cl?.Name} for device {device.Name}!");

            return Ok(AnswerExtensions.Success(true));
        }

        /// <summary>
        /// Gets a screenshot for the given device
        /// </summary>
        /// <param name="deviceId">The selected device</param>
        /// <param name="fileName">The screenshot filename</param>
        /// <returns>Ok on success</returns>
        [HttpGet("recieve_screenshot/{deviceId}/{fileName}")]
        public async Task<IActionResult> ReceiveScreenshot(string deviceId, string fileName)
        {
            if (string.IsNullOrEmpty(deviceId))
                return BadRequest(AnswerExtensions.Fail("Invalid device data"));

            try
            {
                string screenshotFilePath = System.IO.Path.Combine(Config.SCREENSHOTS_PATH, deviceId, $"{fileName}.png");
                var data = await System.IO.File.ReadAllBytesAsync(screenshotFilePath);

                return File(data, "image/png");
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
                _logger.LogInformation($"Clearing log of {device.Name} ...");

                device.DeviceLog.Clear();
                await _context.SaveChangesAsync();

                _clientService.NotifyClientQueues(EventQueueItem.EventKind.LogCleared, deviceID);
                return Ok(AnswerExtensions.Success("ok"));
            }
            else
                return NotFound(AnswerExtensions.Fail("Device not found!"));
        }

        /// <summary>
        /// Clears the device hardware log of the given device completely
        /// </summary>
        /// <param name="deviceID">The selected device</param>
        /// <returns>Ok on success</returns>
        [HttpGet("clear_hw_changes/{deviceID}")]
        public async Task<IActionResult> ClearDeviceHardwareChangesAsync(string deviceID)
        {
            if (string.IsNullOrEmpty(deviceID))
                return BadRequest(AnswerExtensions.Fail("Invalid device data"));

            var device = await _context.Device.Include(d => d.DeviceLog).Where(p => p.Guid == deviceID).FirstOrDefaultAsync();
            if (device != null)
            {
                _logger.LogInformation($"Clearing hardware changes of {device.Name} ...");

                device.DeviceChange.Clear();
                await _context.SaveChangesAsync();

                _clientService.NotifyClientQueues(EventQueueItem.EventKind.HardwareChangesCleared, deviceID);
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

            device.DeviceMessage.Add(new home.Models.DeviceMessage() { Content = message.Content, Title = message.Title, Type = (short)message.Type, Timestamp = DateTime.Now, IsRecieved = false });

            LogEntry.LogLevel level = LogEntry.LogLevel.Information;
            switch (message.Type)
            {
                case Message.MessageImage.Information: level = LogEntry.LogLevel.Information; break;
                case Message.MessageImage.Warning: level = LogEntry.LogLevel.Warning; break;
                case Message.MessageImage.Error: level = LogEntry.LogLevel.Error; break;
            }

            await _context.DeviceLog.AddAsync(ModelConverter.CreateLogEntry(device, $"Received message: {message}", level, false));
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Sent message to {device.Name}: {message}");

            _clientService.NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, device);
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
            var device = await _context.GetDeviceByIdAsync(deviceId);
            if (device == null)
                return BadRequest(AnswerExtensions.Fail($"Device couldn't be found: {deviceId}"));

            if (!device.Status)
                return BadRequest(AnswerExtensions.Fail("Cannot set live status if device is offline!"));

            if (device.Ostype == (int)Model.Device.OSType.Android)
                return BadRequest(AnswerExtensions.Fail("Cannot set live status if device is an Android device!"));

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
            var logEntry = ModelConverter.CreateLogEntry(device, $"Device \"{device.Name}\" status changed to {(live ? "live" : "normal")} by client {client.Name}!", LogEntry.LogLevel.Information, false);

            await _context.DeviceLog.AddAsync(logEntry);
            await _context.SaveChangesAsync();

            _clientService.NotifyClientQueues(EventQueueItem.EventKind.LiveModeChanged, device);

            return Ok(AnswerExtensions.Success("ok"));
        }

        /// <summary>
        /// Deletes a device from the database completely
        /// </summary>
        /// <param name="guid">The id of the device</param>
        /// <returns>Ok on success</returns>
        [HttpDelete("delete/{guid}")]
        public async Task<IActionResult> DeleteDeviceAsync(string guid)
        {
            var device = await DeviceHelper.GetDeviceByIdAsync(_context, guid);

            if (device != null)
            {
                string deviceName = device.Name;

                foreach (var item in device.DeviceChange)
                    _context.DeviceChange.Remove(item);

                foreach (var item in device.DeviceCommand)
                    _context.DeviceCommand.Remove(item);

                foreach (var item in device.DeviceDiskDrive)
                    _context.DeviceDiskDrive.Remove(item);

                foreach (var item in device.DeviceLog)
                    _context.DeviceLog.Remove(item);

                foreach (var item in device.DeviceGraphic)
                    _context.DeviceGraphic.Remove(item);

                foreach (var item in device.DeviceMessage)
                    _context.DeviceMessage.Remove(item);

                foreach (var item in device.DeviceScreen)
                    _context.DeviceScreen.Remove(item);

                foreach (var item in device.DeviceBios)
                    _context.DeviceBios.Remove(item);

                foreach (var item in device.DeviceScreenshot)
                    _context.DeviceScreenshot.Remove(item);

                foreach (var item in device.DeviceWarning)
                    _context.DeviceWarning.Remove(item);

                if (device.DeviceUsage != null)
                    _context.DeviceUsage.Remove(device.DeviceUsage);

                _context.Device.Remove(device);
                _context.DeviceEnvironment.Remove(device.Environment);

                // Delete screenshots
                int count = 0;
                var screenshotsFolder = new System.IO.DirectoryInfo(System.IO.Path.Combine(Config.SCREENSHOTS_PATH, device.Guid));
                if (screenshotsFolder.Exists)
                {
                    List<FileInfo> filesToDelete = new List<FileInfo>();
                    foreach (var screenshot in screenshotsFolder.EnumerateFiles("*.png"))
                        filesToDelete.Add(screenshot);

                    count = filesToDelete.Count;
                    if (filesToDelete.Count > 0)
                    {
                        foreach (var file in filesToDelete)
                        {
                            try
                            {
                                file.Delete();
                            }
                            catch
                            {
                                // ignore
                            }
                        }
                    }

                    try
                    {
                        screenshotsFolder.Delete();
                    }
                    catch
                    {
                        // ignore
                    }

                    // Clean up list
                    filesToDelete.Clear();
                }

                try
                {
                    _clientService.NotifyClientQueues(EventQueueItem.EventKind.DeviceDeleted, device.Guid);
                    await _context.SaveChangesAsync();
                    await _webhookService.PostWebHookAsync(WebhookAPI.Webhook.LogLevel.Success, $"Device \"{deviceName}\" removed! (Deleted {count} screenshots!)", device.Name);
                }
                catch (Exception ex)
                {
                    await _webhookService.PostWebHookAsync(WebhookAPI.Webhook.LogLevel.Error, $"Failed to remove \"{deviceName}\": {ex}", device.Name);
                }
                return Ok(AnswerExtensions.Success("ok"));
            }
            else
                return BadRequest(AnswerExtensions.Fail("This device doesn't exists!"));
        }

        /// <summary>
        /// Sends a command to the given device
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>Ok on success</returns>
        [HttpPost("send_command")]
        public async Task<IActionResult> SendCommandAsync([FromBody] Command command)
        {
            if (command == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device data!"));

            // Check if this devices exists
            home.Models.Device device = await _context.GetDeviceByIdAsync(command.DeviceID);

            if (device == null)
                return BadRequest(AnswerExtensions.Fail("Device doesn't exists!"));

            if (!device.Status)
                return BadRequest(AnswerExtensions.Fail("Device is offline, you cannot send commands!"));

            device.DeviceCommand.Add(new home.Models.DeviceCommand() { Executable = command.Executable, IsExceuted = false, Parameter = command.Parameter, Timestamp = DateTime.Now });
            await _context.DeviceLog.AddAsync(ModelConverter.CreateLogEntry(device, $"Received command: {command}", LogEntry.LogLevel.Information, false));

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Sent command to {device.Name}: {command}");
            _clientService.NotifyClientQueues(EventQueueItem.EventKind.LogEntriesRecieved, device);

            return Ok(AnswerExtensions.Success("ok"));
        }

        /// <summary>
        /// Shuts down all devices specified via config
        /// </summary>
        /// <param name="code">The security code to prevent unauthorized user from calling this API-method</param>
        /// <param name="reason">The reason why this shutdown was executed; e.g. test or USV</param>
        /// <param name="shutdown_all">True if all devices should be shutdown</param>
        /// <returns>Ok on success</returns>
        [HttpGet("broadcast_shutdown")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendBroadcastShutdownAsync(string code, string reason, bool shutdown_all)
        {
            var now = DateTime.Now;

            if (Program.GlobalConfig.BroadcastShutdownConfig == null || !Program.GlobalConfig.BroadcastShutdownConfig.IsActive)
                return StatusCode(StatusCodes.Status403Forbidden);

            if (Program.GlobalConfig.BroadcastShutdownConfig.SecurityCode != code)
                return StatusCode(StatusCodes.Status401Unauthorized);

            var entries = Program.GlobalConfig.BroadcastShutdownConfig.Entries;
            var devices = await _context.GetAllDevicesAsync(false);

            // Send also to webhook
            await _webhookService.PostWebHookAsync(WebhookAPI.Webhook.LogLevel.Info, "Broadcast shutdown executed due to the following reason: {}. Devices: {}", "BroadcastShutdown");

            if (!shutdown_all)
            {
                if (entries != null && entries.Length > 0)
                {
                    List<Device> tmp = new List<Device>();

                    foreach (var entry in entries)
                    {
                        var d = devices.FirstOrDefault(d => d.DeviceGroup == entry || d.Guid == entry || d.Name == entry);
                        if (d != null)
                            tmp.Add(d);
                    }

                    devices = tmp;
                }
            }

            if (devices.Count > 0)
            {
                int count = 0;
                _logger.LogInformation($"Executing Broadcast Shutdown due to {reason}!");

                foreach (var device in devices)
                {
                    var type = ModelConverter.ConvertDevice(device).OS;
                    if (type.IsAndroid())
                    {
                        _logger.LogWarning($"[Broadcast Shutdown]: Skipping executing of shutdown command {device.Name} is an Android device and doesn't supports this command!");
                        continue;
                    }
                    else if (!device.Status)
                    {
                        _logger.LogWarning($"[Broadcast Shutdown]: Skipping executing of shutdown command: {device.Name} is not active!");
                        continue;
                    }
                    else
                    {
                        void ShutdownDevice(Device device)
                        {
                            string message = $"[Broadcast Shutdown]: Shutdown device {device.Name} ...";
                            _logger.LogInformation(message);

                            // Shutdown the device via command!
                            var command = Home.API.Helper.GeneralHelper.GetShutdownCommand(false, type);
                            device.DeviceCommand.Add(new DeviceCommand() { Executable = command.Item1, Parameter = command.Item2, Timestamp = now });
                            device.DeviceLog.Add(new DeviceLog() { Blob = message, LogLevel = (int)LogLevel.Information, Timestamp = now });
                        }

                        if (device.Name == Environment.MachineName)
                        {
                            // Delay execution for this machine for one minute (to ensure that all devices have received the ack command before the current machine is down)
                            await Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith((p) =>
                            {
                                ShutdownDevice(device);
                            }).ConfigureAwait(false);
                            count++;
                        }
                        else
                        {
                            ShutdownDevice(device);
                            count++;
                        }
                    }
                }

                if (count > 0)
                {
                    _logger.LogInformation($"[Broadcast Shutdown]: Shutdown {count} devices!");
                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            else
                return StatusCode(StatusCodes.Status404NotFound);
        }

        #region Update Device Scheduling Rules

        /// <summary>
        /// Returns all existing scheduling rules
        /// </summary>
        /// <returns>All previously scheduling rules (if any)</returns>
        [HttpGet("SchedulingRules")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetSchedulingRules()
        {
            try
            {
                if (!System.IO.File.Exists(Program.DeviceSchedulingRulesPath))
                    return NoContent();

                return Ok(DeviceSchedulingRule.Load(Program.DeviceSchedulingRulesPath));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Replaces all scheduling rules with the parameter given
        /// </summary>
        /// <param name="rules">All scheduling rules to save</param>
        /// <returns>Ok on success</returns>
        [HttpPost("UpdateSchedulingRules")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult UpdateSchedulingRules(IEnumerable<DeviceSchedulingRule> rules)
        {
            try
            {
                DeviceSchedulingRule.Save(rules, Program.DeviceSchedulingRulesPath);
                DeviceScheduleService.UpdateSchedulingRules = true;
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        #endregion

        /// <summary>
        /// Can be called to test the connection (also makes a dummy db call)
        /// </summary>
        /// <returns>Ok on success</returns>
        [HttpGet("test")]
        public IActionResult ConnectionTest()
        {
            // Also ensure that the db connection is working (test)
            var dummy = _context.Device.FirstOrDefault(d => d.Name == "test");  
            return Ok();
        }
    }
}