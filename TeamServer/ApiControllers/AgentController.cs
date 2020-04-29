using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using TeamServer.Models;

namespace TeamServer.ApiControllers
{
    [Authorize]
    [Route("api/agent")]
    public class AgentsController : Controller
    {
        [HttpGet]
        public SessionData[] GetConnectedAgents()
        {
            return Program.ServerController.GetConnectedAgents();
        }

        [HttpGet("events")]
        public AgentEvent[] GetAgentEvents()
        {
            return Program.ServerController.GetAgentEvents();
        }

        [HttpPost("commandrequest")]
        public void IssueAgentCommand([FromBody]AgentCommandRequest request)
        {
            Program.ServerController.SendAgentCommand(request);
        }
    }
}