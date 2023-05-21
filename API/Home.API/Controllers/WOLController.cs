using Home.API.Helper;
using Home.API.home;
using Home.API.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Home.API.Controllers
{
    /// <summary>
    /// Can be used to send WOL Requests to a client (Magic Package)
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class WOLController : ControllerBase
    {
        private readonly ILogger<WOLController> _logger;
        private readonly IWOLService _wolService;
        private readonly HomeContext _context;

        public WOLController(ILogger<WOLController> logger, IWOLService wolService, HomeContext context) 
        {
            _logger = logger;
            _wolService = wolService;
            _context = context;
        }

        /// <summary>
        /// Sends the magick package to given client
        /// </summary>
        /// <param name="macAddress">The mac address in hex format</param>
        /// <param name="port">The port from the settings will be used: Should be 7 or 9</param>
        /// <returns></returns>
        [HttpGet("SendWakeUpRequest/{macAddress}/{port:int?}")]
        public async Task<bool> SendWakeUpRequestAsync(string macAddress, int? port = null)
        {
            if (port != null)
                await _wolService.SendWOLRequestAsync(macAddress, port.Value);
            else
                await _wolService.SendWOLRequestAsync(macAddress);

            return true;
        }

        /// <summary>
        /// Wakes up a device by it's device id (using mac address from db)
        /// </summary>
        /// <param name="deviceId">The given device (id)</param>
        /// <returns></returns>
        [HttpGet("{deviceId}/SendWakeUpRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendWakeUpRequestAsync(string deviceId)
        {
            var device = await _context.GetDeviceByIdAsync(deviceId);

            if (device == null)
                return NotFound();

            if (string.IsNullOrEmpty(device.MacAddress))
                 return BadRequest("No mac address known for this device");

            await _wolService.SendWOLRequestAsync(device.MacAddress);
            return Ok();    
        }
    }
}