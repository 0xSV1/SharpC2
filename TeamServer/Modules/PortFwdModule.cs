using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

using TeamServer.Models;
using TeamServer.Interfaces;
using TeamServer.Controllers;

namespace TeamServer.Modules
{
    internal class PortFwdModule : IServerModule
    {
        ServerController serverController;
        AgentController agentController;

        public ServerModuleInfo GetModuleInfo()
        {
            return new ServerModuleInfo
            {
                Name = "ReversePortForward",
                Developer = "Daniel Duggan, Adam Chester",
                Commands = new List<ServerModuleInfo.Command>
                {
                    new ServerModuleInfo.Command
                    {
                        Name = "DataFromAgent",
                        Description = "Handles receiving data from the Agent and relaying data back",
                        CallBack = HandleDataFromAgent
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

        private void HandleDataFromAgent(string data, SessionData sessionData)
        {
            var split = data.Split('|');
            var chunkId = split[0];
            var forwardHost = IPAddress.Parse(split[1]);
            var forwardPort = Convert.ToInt32(split[2]);
            var httpData = Convert.FromBase64String(split[3]);

            serverController.LogWebRequest(Encoding.UTF8.GetString(httpData));

            var t = new Thread(() =>
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
                {
                    var buffer = new byte[1024];
                    try
                    {
                        socket.Connect(new IPEndPoint(forwardHost, forwardPort));
                        socket.Send(httpData);
                        socket.Receive(buffer);

                        var req = new AgentCommandRequest
                        {
                            AgentId = sessionData.AgentId,
                            Module = "rportfwd",
                            Command = "DataFromTeamServer",
                            Data = string.Format("{0}|{1}", chunkId, Encoding.UTF8.GetString(buffer))
                        };

                        serverController.SendAgentCommand(req);
                    }
                    catch { }
                }
            });

            t.Start();
        }
    }
}