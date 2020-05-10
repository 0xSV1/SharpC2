using System;
using System.Linq;
using TeamServer.Models;

using Agent.Common;

namespace TeamServer.UI
{
    public class CommandHandler
    {
        public static void SendCommand(string agentId, string command)
        {
            var split = command.Split(" ");
            var module = split[0];

            if (module.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                var loadedModules = Program.ServerController.GetConnectedAgents().Where(s => s.AgentId.Equals(agentId)).Select(s => s.loadedModules).FirstOrDefault();
                var helpText = new SharpC2ResultList<AgentHelpResult>();
                foreach (var loadedModule in loadedModules)
                {
                    foreach (var agentCommand in loadedModule.Commands)
                    {
                        if (agentCommand.Visible == true)
                        {
                            helpText.Add(new AgentHelpResult
                            {
                                Module = loadedModule.Name,
                                Command = agentCommand.Name,
                                Description = agentCommand.Description,
                                HelpText = agentCommand.HelpText,
                            });
                        }
                    }
                }
                Program.ServerController.AddAgentEvent(new AgentEvent
                {
                    AgentId = agentId,
                    EventTime = DateTime.UtcNow,
                    EventType = AgentEvent.AgentEventType.AgentHelpRequest,
                    Data = helpText.ToString()
                });
            }
            else
            {
                var cmd = split[1];
                var data = string.Join(" ", split[2..]);

                var req = new AgentCommandRequest
                {
                    AgentId = agentId,
                    Module = module,
                    Command = cmd,
                    Data = data
                };

                Program.ServerController.SendAgentCommand(req);
            }
        }
    }
}
