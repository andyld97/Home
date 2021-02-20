using Home.Data;
using Home.Data.Events;
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
            return Ok();
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
    }
}
