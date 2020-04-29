using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using TeamServer.Models;

namespace TeamServer.ApiControllers
{
    [Authorize]
    [Route("api/server")]
    public class ServerController : Controller
    {
        [HttpGet]
        public ServerModuleInfo[] Get()
        {
            return Program.ServerController.GetServerModulesInfo();
        }
    }
}