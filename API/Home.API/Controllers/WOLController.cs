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

        public WOLController(ILogger<WOLController> logger, IWOLService wolService) 
        {
            _logger = logger;
            _wolService = wolService;
        }

        /// <summary>
        /// Sends the magick package to to given client
        /// </summary>
        /// <param name="macAddress">The mac address in hexformat</param>
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
    }
}
