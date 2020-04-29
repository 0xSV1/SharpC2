using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using TeamServer.Models;

namespace TeamServer.ApiControllers
{
    [Authorize]
    [Route("api/comm")]
    public class CommsController : Controller
    {
        [HttpGet]
        public ModuleInformation Get()
        {
            return Program.ServerController.GetCommModuleInfo();
        }

        [HttpGet("logs")]
        public string[] GetWebLogs()
        {
            return Program.ServerController.GetWebRequests();
        }
    }
}