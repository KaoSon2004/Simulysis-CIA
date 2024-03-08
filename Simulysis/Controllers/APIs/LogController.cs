using Entities.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Simulysis.Controllers.APIs
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class LogController : ControllerBase
    {
        [HttpPost]
        public IActionResult GetLog([FromBody] JObject message)
        {
            string _mess = JsonConvert.SerializeObject(message);
            Loggers.SVP.Info(_mess);
            return Ok("Logged");
        }
    }
}
