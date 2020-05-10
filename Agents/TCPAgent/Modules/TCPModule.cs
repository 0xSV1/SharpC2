using System.Collections.Generic;

using Agent.Models;
using Agent.Interfaces;
using Agent.Controllers;

namespace Agent.Modules
{
    class TCPModule : IAgentModule
    {
        public AgentModuleInfo GetModuleInfo()
        {
            return new AgentModuleInfo
            {
                Name = "Stage",
                Developer = "Daniel Duggan",
                Commands = new List<AgentModuleInfo.AgentCommand>
                {
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "Link",
                        Description = "Handle incoming link request",
                        Visible = false,
                        Callback = HandleLinkRequest
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "Unlink",
                        Description = "Handle disconnected from a parent agent",
                        Visible = false,
                        Callback = HandleUnlinkRequest
                    }
                }
            };
        }

        public void Initialise(AgentController agent, ConfigController config)
        {
            var info = GetModuleInfo();
            agent.RegisterAgentModule(info);
        }

        private void HandleLinkRequest(string data, AgentController agent, ConfigController config)
        {
            var metadata = Helpers.GetInitialMetadata((string)config.GetOption(ConfigurationSettings.AgentId));
            metadata.ParentAgentId = data;
            agent.InitialCheckIn(metadata);
            agent.AddAgentModule(new CoreAgentModule());
        }

        private void HandleUnlinkRequest(string data, AgentController agentController, ConfigController configController)
        {
            
        }
    }
}