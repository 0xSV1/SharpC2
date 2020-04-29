using TeamServer.Models;
using TeamServer.Controllers;

namespace TeamServer.Interfaces
{
    internal interface IServerModule
    {
        void Initialise(ServerController serverController, AgentController agentController);
        ServerModuleInfo GetModuleInfo();
    }
}