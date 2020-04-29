using System.Threading;
using System.Collections.Generic;

using TeamServer.Models;
using TeamServer.Interfaces;

namespace TeamServer.Controllers
{
    internal class ServerController
    {
        protected AgentController agentController { get; set; }
        protected ICommModule commModule { get; set; }
        protected ServerControllerStatus serverControllerStatus { get; set; }

        internal delegate void OnServerCommand(string data, SessionData sessionData);
        protected List<ServerModuleInfo> modules = new List<ServerModuleInfo>();
        protected Dictionary<string, Dictionary<string, OnServerCommand>> commands = new Dictionary<string, Dictionary<string, OnServerCommand>>();
        
        protected List<string> WebLogs = new List<string>();

        internal ServerController(AgentController agentController)
        {
            this.agentController = agentController;
        }

        internal void AddServerModule(IServerModule module)
        {
            module.Initialise(this, agentController);
        }

        internal void SetCommModule(ICommModule module)
        {
            commModule = module;
        }

        internal void Start()
        {
            commModule.Initialise(agentController);
            var threadChannel = new Queue<C2Data>();
            commModule.Start();
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
            return commModule.GetModuleInfo();
        }

        internal ServerModuleInfo[] GetServerModulesInfo()
        {
            return modules.ToArray();
        }

        internal SessionData[] GetConnectedAgents()
        {
            return agentController.sessionCache.ToArray();
        }

        internal void LogWebRequest(string request)
        {
            WebLogs.Add(request.Replace("\r\n", " ").Replace("\0", "").TrimEnd());
        }

        internal string[] GetWebRequests()
        {
            return WebLogs.ToArray();
        }

        protected void ServerThread(object obj)
        {
            var c2Datas = obj as Queue<C2Data>;
            while (serverControllerStatus == ServerControllerStatus.Started)
            {
                // Handle sending of data from queue
                if (c2Datas.Count > 0)
                {
                    var c2DataOut = c2Datas.Dequeue();
                    commModule.SendData(c2DataOut);
                }

                // Handle receiving of data from queue
                C2Data c2DataIn;
                if (commModule.RecvData(out c2DataIn))
                {
                    HandleServerCall(c2DataIn);
                }
            }
        }

        protected void HandleServerCall(C2Data data)
        {
            if (commands.ContainsKey(data.Module))
                if (commands[data.Module].ContainsKey(data.Command))
                {
                    var sessionData = agentController.GetSession(data.AgentId);
                    commands[data.Module][data.Command](data.Data, sessionData);
                }
        }

        internal void SendAgentCommand(AgentCommandRequest request)
        {
            commModule.SendData(new C2Data()
            {
                AgentId = request.AgentId,
                Module = request.Module,
                Command = request.Command,
                Data = request.Data
            });
        }

        internal void AddAgentEvent(AgentEvent e)
        {
            agentController.agentEvents.Add(e);
        }

        internal AgentEvent[] GetAgentEvents()
        {
            return agentController.GetAgentEvents();
        }
    }
}