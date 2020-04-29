using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;

using TeamServer.Models;
using TeamServer.Interfaces;
using TeamServer.Controllers;

namespace TeamServer.Modules
{
    internal class CoreServerModule : IServerModule
    {
        ServerController serverController;
        AgentController agentController;

        public ServerModuleInfo GetModuleInfo()
        {
            return new ServerModuleInfo
            {
                Name = "Core",
                Developer = "Daniel Duggan, Adam Chester",
                Commands = new List<ServerModuleInfo.Command>
                {
                    new ServerModuleInfo.Command
                    {
                        Name = "InitialCheckin",
                        Description = "Handles the initial checkin of an Agent.",
                        CallBack = HandleAgentCheckin
                    },
                    new ServerModuleInfo.Command
                    {
                        Name = "CommandOutput",
                        Description = "Handles the command output from an Agent.",
                        CallBack = HandleCommandOutput
                    },
                    new ServerModuleInfo.Command
                    {
                        Name = "AgentError",
                        Description = "Handles error messages from an Agent.",
                        CallBack = HandleErrorMessages
                    },
                    new ServerModuleInfo.Command
                    {
                        Name = "ModuleRegistered",
                        Description = "Registers an Agent Module with the Team Server.",
                        CallBack = RegisterAgentModule
                    },
                    new ServerModuleInfo.Command
                    {
                        Name = "NOP",
                        Description = "Handles a standard Agent checkin.",
                        CallBack = HandleAgentNop
                    }
                }
            };
        }

        public void Initialise(ServerController serverController, AgentController agentController)
        {
            this.serverController = serverController;
            this.agentController = agentController;

            var moduleInfo = GetModuleInfo();
            serverController.RegisterServerModule(moduleInfo);
        }

        protected void HandleAgentCheckin(string data, SessionData sessionData)
        {
            var metadata = new InitialMetadata();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var ser = new DataContractJsonSerializer(metadata.GetType());
            metadata = ser.ReadObject(ms) as InitialMetadata;
            ms.Close();

            agentController.CreateSession(metadata);
            
            agentController.agentEvents.Add(new AgentEvent {
                AgentId = metadata.AgentId,
                EventTime = DateTime.UtcNow,
                EventType = AgentEvent.AgentEventType.AgentConnected
            });
        }

        protected void HandleCommandOutput(string data, SessionData sessionData)
        {
            serverController.AddAgentEvent(new AgentEvent
            {
                AgentId = sessionData.AgentId,
                EventTime = DateTime.UtcNow,
                EventType = AgentEvent.AgentEventType.AgentCommandResponse,
                Data = data
            });
        }

        protected void HandleErrorMessages(string data, SessionData sessionData)
        {
            serverController.AddAgentEvent(new AgentEvent
            {
                AgentId = sessionData.AgentId,
                EventTime = DateTime.UtcNow,
                EventType = AgentEvent.AgentEventType.AgentErrorMessage,
                Data = data
            });
        }

        protected void RegisterAgentModule(string data, SessionData sessionData)
        {
            var moduleInfo = new AgentModuleInfo();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var ser = new DataContractJsonSerializer(moduleInfo.GetType());
            moduleInfo = ser.ReadObject(ms) as AgentModuleInfo;
            ms.Close();

            var session = agentController.GetSession(sessionData.AgentId);

            if (session.loadedModules == null)
                session.loadedModules = new List<AgentModuleInfo> { moduleInfo };
            else
                session.loadedModules.Add(moduleInfo);

            agentController.UpdateSession(sessionData.AgentId, session);

            serverController.AddAgentEvent(new AgentEvent
            {
                AgentId = sessionData.AgentId,
                EventTime = DateTime.UtcNow,
                EventType = AgentEvent.AgentEventType.AgentModuleRegistered,
                Data = moduleInfo.Name
            });
        }

        protected void HandleAgentNop(string data, SessionData sessionData)
        {
            sessionData.LastCheckIn = DateTime.UtcNow;
            agentController.UpdateSession(sessionData.AgentId, sessionData);
        }
    }
}