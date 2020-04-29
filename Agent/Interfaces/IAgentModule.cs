using Agent.Models;
using Agent.Controllers;

namespace Agent.Interfaces
{
    public interface IAgentModule
    {
        AgentModuleInfo GetModuleInfo();
        void Initialise(AgentController agent, ConfigurationController config);
    }
}