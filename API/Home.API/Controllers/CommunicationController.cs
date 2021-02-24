using Home.Data;
using Home.Data.Events;
using Home.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
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
                return NotFound(AnswerExtensions.Fail("Invalid client data"));

            if (!Program.Clients.Any(p => p.ID == client.ID))
            {
                Program.Clients.Add(client);
                Program.EventQueues.Add(new Data.Events.EventQueue() { ClientID = client.ID, LastClientRequest = DateTime.Now });

                _logger.LogInformation($"Client {client} has just logged in!");
            }
            else
                return BadRequest(AnswerExtensions.Fail("Already logged in"));

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
                }

                lock (Program.EventQueues)
                {
                    EventQueue eventQueue = Program.EventQueues.Where(p => p.ClientID == client.ID).FirstOrDefault();
                    if (eventQueue != null)
                        Program.EventQueues.Remove(eventQueue);
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

            lock (Program.Clients)
            {
                if (!Program.Clients.Any(p => p.ID == clientId))
                    return NotFound("Client not found!");
            }

            lock (Program.Devices)
            {
                if (!Program.Devices.Any(p => p.ID == deviceID))
                    return NotFound("Device not found!");

                var device = Program.Devices.Where(p => p.ID == deviceID).FirstOrDefault();
                if (device != null)
                {
                    device.IsScreenshotRequired = true;
                    _logger.LogInformation($"Aquired screenshot from {clientId} for device {deviceID}");
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
                return BadRequest(AnswerExtensions.Fail<Screenshot>(ex.Message));
            }
        }
    }
}