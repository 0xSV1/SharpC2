using System;
using System.Threading;
using System.Collections.Generic;

using C2.Models;

using TeamServer.Models;
using TeamServer.Interfaces;
using System.Runtime.InteropServices.WindowsRuntime;

namespace TeamServer.Controllers
{
    internal class ServerController
    {
        protected AgentController AgentController { get; set; }
        protected ICommModule CommModule { get; set; }
        protected ServerControllerStatus ServerControllerStatus { get; set; }

        internal delegate void OnServerCommand(string data, SessionData sessionData);
        protected List<ServerModuleInfo> modules = new List<ServerModuleInfo>();
        protected Dictionary<string, Dictionary<string, OnServerCommand>> commands = new Dictionary<string, Dictionary<string, OnServerCommand>>();

        protected List<WebLog> webLogs = new List<WebLog>();

        internal ServerController(AgentController AgentController)
        {
            this.AgentController = AgentController;
        }

        internal void AddServerModule(IServerModule module)
        {
            module.Initialise(this, AgentController);
        }

        internal void SetCommModule(ICommModule module)
        {
            CommModule = module;
        }

        internal void Start()
        {
            CommModule.Initialise(AgentController);
            var threadChannel = new Queue<AgentMessage>();
            CommModule.Start();
            var t = new Thread(ServerThread);
            t.Start(threadChannel);
        }

        internal void RegisterServerModule(ServerModuleInfo module)
        {
            modules.Add(module);

            commands.Add(module.Name, new Dictionary<string, OnServerCommand>());
            foreach (var command in module.Commands)
                commands[module.Name].Add(command.Name, command.CallBack);
        }

        internal ModuleInformation GetCommModuleInfo()
        {
            return CommModule.GetModuleInfo();
        }

        internal ServerModuleInfo[] GetServerModulesInfo()
        {
            return modules.ToArray();
        }

        internal SessionData[] GetConnectedAgents()
        {
            return AgentController.sessionCache.ToArray();
        }

        protected void ServerThread(object obj)
        {
            var agentMessages = obj as Queue<AgentMessage>;
            while (ServerControllerStatus == ServerControllerStatus.Started)
            {
                // Handle sending of data from queue
                if (agentMessages.Count > 0)
                {
                    var agentMessageOut = agentMessages.Dequeue();
                    CommModule.SendData(agentMessageOut);
                }

                // Handle receiving of data from queue
                if (CommModule.RecvData(out AgentMessage agentMessageIn))
                {
                    var c2Data = C2.Helpers.Deserialise<C2Data>(agentMessageIn.Data);
                    HandleServerCall(c2Data);
                }
            }
        }

        protected void HandleServerCall(C2Data data)
        {
            if (commands.ContainsKey(data.Module))
                if (commands[data.Module].ContainsKey(data.Command))
                {
                    var sessionData = AgentController.GetSession(data.AgentId);
                    commands[data.Module][data.Command](data.Data, sessionData);
                }
        }

        internal void SendAgentCommand(AgentCommandRequest request)
        {
            var c2Data = new C2Data
            {
                AgentId = request.AgentId,
                Module = request.Module,
                Command = request.Command,
                Data = request.Data
            };

            var message = new AgentMessage
            {
                AgentId = request.AgentId,
                Data = Convert.ToBase64String(C2.Helpers.Serialise(c2Data))
            };

            // If the Agent is a P2P, the message needs to go to its upmost parent.
            var parent = request.AgentId;
            while (!string.IsNullOrEmpty(parent))
            {
                var session = AgentController.GetSession(parent);
                parent = session.ParentAgentId;
                if (string.IsNullOrEmpty(parent))
                    message.AgentId = session.AgentId;
            }

            CommModule.SendData(message);

            AddAgentEvent(new AgentEvent
            {
                AgentId = request.AgentId,
                EventTime = DateTime.UtcNow,
                EventType = AgentEvent.AgentEventType.AgentCommandRequest,
                Data = string.Format("{0} {1} {2}", request.Module, request.Command, request.Data)
            });
        }

        internal void AddAgentEvent(AgentEvent e)
        {
            AgentController.agentEvents.Add(e);
        }

        internal AgentEvent[] GetAgentEvents(string agentId)
        {
            return AgentController.GetAgentEvents(agentId);
        }

        internal void AddWebLog(WebLog log)
        {
            webLogs.Add(log);
        }

        internal WebLog[] GetWebLogs()
        {
            return webLogs.ToArray();
        }
    }
}