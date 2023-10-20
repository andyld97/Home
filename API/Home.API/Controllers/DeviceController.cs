using Home.API.Helper;
using Home.API.home;
using Home.API.home.Models;
using Home.API.Services;
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
using Newtonsoft.Json.Linq;
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
        private readonly IClientService _clientService;
        private readonly IDeviceAckService _deviceAckService;

        public DeviceController(ILogger<DeviceController> logger, HomeContext homeContext, IClientService clientService, IDeviceAckService deviceAckService)
        {
            _logger = logger;
            _context = homeContext;
            _clientService = clientService;
            _deviceAckService = deviceAckService;
        }

        /// <summary>
        /// Gets all devices
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var devices = await DeviceHelper.GetAllDevicesAsync(_context, true);

            List<Device> devicesList = new List<Device>();
            foreach (var device in devices)
                devicesList.Add(ModelConverter.ConvertDevice(device));

            return Ok(devicesList);
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetInfoAsync()
        {
            var devices = await DeviceHelper.GetAllDevicesAsync(_context, true);
            int devicesOnline = devices.Where(p => p.Status).Count();
            int devicesOffline = devices.Where(p => !p.Status).Count();
            int warnings = devices.Sum(p => p.DeviceWarning.Count);

            JObject result = new JObject();
            result["online"] = devicesOnline;
            result["offline"] = devicesOffline;
            result["warnings"] = warnings;

            return Content(result.ToString(Formatting.None), "application/json");
        }

        /// <summary>
        /// Registers a new device in the database
        /// </summary>
        /// <param name="device">The device to register</param>
        /// <returns>OK on success</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Device device)
        {
            var now = DateTime.Now;

            if (device == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device given"));

            // Check if device exists
            if (!(await _context.Device.AnyAsync(p => p.Guid == device.ID)))
            {
                try
                {
                    device.Status = Device.DeviceStatus.Active;
                    device.LastSeen = now;
                    device.LogEntries.Clear();
                    var logEntry = new LogEntry(now, $"Device {device.Name} was successfully added!", LogEntry.LogLevel.Information, true);
                    device.LogEntries.Add(logEntry);
                    _logger.LogInformation($"New device {device.Environment.MachineName} has just registered!");
                    device.IsScreenshotRequired = true;

                    var dbDevice = ModelConverter.ConvertDevice(_context, _logger, device);
                    dbDevice.IsScreenshotRequired = true;
                    await _context.Device.AddAsync(dbDevice);
                    await _context.DeviceChange.AddAsync(new DeviceChange() { Timestamp = now, Device = dbDevice, Description = $"Device \"{dbDevice.Name}\" added to the system initially!" });
                    await _context.SaveChangesAsync();

                    // To notify webhook:
                    ModelConverter.ConvertLogEntry(dbDevice, logEntry);

                    _clientService.NotifyClientQueues(EventQueueItem.EventKind.NewDeviceConnected, device);
                    return Ok(AnswerExtensions.Success(true));
                }
                catch (Exception ex)
                {
                    string message = $"Failed to register device: {ex.ToString()}";
                    _logger.LogError(message);
                    Program.WebHookLogging.Enqueue((WebhookAPI.Webhook.LogLevel.Error, message, device.Name));
                    return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
                }
            }

            return BadRequest(AnswerExtensions.Fail("Device-Register couldn't be processed!"));
        }

        /// <summary>
        /// Processes an ack sent from a device
        /// </summary>
        /// <param name="device">The device which has sent the ack</param>
        /// <returns>OK on success</returns>
        [HttpPost("ack")]
        public async Task<IActionResult> ProcessDeviceAckAsync([FromBody] Home.Model.Device device)
        {            
            if (device == null)
                return BadRequest(AnswerExtensions.Fail("Invalid device given"));

            var result = await _deviceAckService.ProcessDeviceAckAsync(device);

            if (result.Success)
                return Ok(AnswerExtensions.Success(result.AckResult));
            else
                return StatusCode(result.StatusCode, new Answer<AckResult>("fail", new AckResult(AckResult.Ack.Invalid)) { ErrorMessage = result.ErrorMessage });
        }

        /// <summary>
        /// Processes a screenshot for the given device
        /// </summary>
        /// <param name="shot">The screenshot object</param>
        /// <returns>OK on success</returns>
        [HttpPost("screenshot")]
        public async Task<IActionResult> PostScreenshotAsync([FromBody] Screenshot shot)
        {
            var now = DateTime.Now;
            string fileName = now.ToString(Home.Data.Consts.SCREENSHOT_DATE_FILE_FORMAT);

            if (shot == null)
                return BadRequest(AnswerExtensions.Fail("screenshot is null!"));

            if (shot.ScreenIndex != null)
                fileName += $"_{shot.ScreenIndex}";

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
                    byte[] buffer = new byte[Consts.BufferSize];

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
                var ds = new DeviceScreenshot() { Device = deviceFound, ScreenshotFileName = fileName, Timestamp = now };

                // If ScreenIndex is null (either using old clients or for clients which only supports multiple screen)
                // Only Home.Windows.Service currently supports screenshots for multiple screens/screenshots
                // Add the screen to the screenshot to create a link
                if (shot.ScreenIndex is not null)
                {
                    var screen = deviceFound.DeviceScreen.Where(p => p.ScreenIndex == shot.ScreenIndex).FirstOrDefault();
                    if (screen != null)
                    {
                        ds.Screen = screen;

                        var logEntry = ModelConverter.CreateLogEntry(deviceFound, $"Received screenshot from {screen.DeviceName}!", LogEntry.LogLevel.Information);
                        await _context.DeviceLog.AddAsync(logEntry);
                        _logger.LogInformation($"Received screenshot from {deviceFound.Environment.MachineName} [{screen.DeviceName}]");
                    }
                }
                else
                {
                    var logEntry = ModelConverter.CreateLogEntry(deviceFound, "Received screenshot!", LogEntry.LogLevel.Information);
                    await _context.DeviceLog.AddAsync(logEntry);
                    _logger.LogInformation($"Received screenshot from {deviceFound.Environment.MachineName}");
                }

                deviceFound.DeviceScreenshot.Add(ds);
                deviceFound.IsScreenshotRequired = false;

                // Also append to event queue
                _clientService.NotifyClientQueues(EventQueueItem.EventKind.DeviceScreenshotRecieved, deviceFound);

                await _context.SaveChangesAsync();
                return Ok(AnswerExtensions.Success(true));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to post screenshot: {ex}");
                return BadRequest(AnswerExtensions.Fail(ex.Message));
            }
        }
    }
}