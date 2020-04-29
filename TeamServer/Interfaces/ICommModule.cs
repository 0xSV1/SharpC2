using TeamServer.Models;
using TeamServer.Controllers;

namespace TeamServer.Interfaces
{
    internal interface ICommModule
    {
        void Initialise(AgentController agentController);
        void Start();
        bool SendData(C2Data data);
        bool RecvData(out C2Data data);
        ModuleInformation GetModuleInfo();
    }
}