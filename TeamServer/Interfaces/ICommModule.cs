using TeamServer.Models;
using TeamServer.Controllers;

using C2.Models;

namespace TeamServer.Interfaces
{
    internal interface ICommModule
    {
        void Initialise(AgentController agentController);
        void Start();
        bool SendData(AgentMessage message);
        bool RecvData(out AgentMessage message);
        ModuleInformation GetModuleInfo();
    }
}