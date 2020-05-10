using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using C2.Models;

using TeamServer.Models;
using TeamServer.Interfaces;
using TeamServer.Controllers;

namespace TeamServer.Modules
{
    public class HTTPCommModule : ICommModule
    {
        protected AgentController AgentController { get; set; }
        protected Socket Socket { get; set; }

        protected const int ChunkLength = 1024;
        protected Queue<AgentMessage> outboundData = new Queue<AgentMessage>();
        protected Queue<AgentMessage> inboundData = new Queue<AgentMessage>();
        protected Queue<string> outboundChunks = new Queue<string>();

        public void Initialise(AgentController AgentController)
        {
            this.AgentController = AgentController;
        }

        public void Start()
        {
            // Set up our HTTP socket
            Socket = new Socket(SocketType.Stream, ProtocolType.IP);
            Socket.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 8080));
            Socket.Listen(20);

            // Execute within thread to avoid this call from blocking
            Thread t = new Thread(delegate (object param)
            {
                while (true)
                {
                    Socket clientSocket = Socket.Accept();
                    AgentHandler(clientSocket as Socket);
                }
            });

            t.Start(Socket);
        }

        public bool RecvData(out AgentMessage message)
        {
            if (inboundData.Count > 0)
            {
                message = inboundData.Dequeue();
                return true;
            }

            message = null;
            return false;
        }

        public bool SendData(AgentMessage message)
        {
            var chunkId = Helpers.GenerateRandomString(10);
            var sessionData = AgentController.GetSession(message.AgentId);

            // Add "outboundChunks" property if it doesn't exist for this agent
            if (sessionData.datastore.ContainsKey("outboundChunks") == false)
                sessionData.datastore.Add("outboundChunks", new Queue<HTTPChunk>());

            var outboundChunks = sessionData.datastore["outboundChunks"] as Queue<HTTPChunk>;

            for (int i = 0; i <= message.Data.Length; i += ChunkLength)
            {
                if (i + ChunkLength >= message.Data.Length)
                {
                    outboundChunks.Enqueue(new HTTPChunk()
                    {
                        Id = chunkId,
                        Data = message.Data[i..],
                        Length = message.Data.Length - i,
                        Final = true
                    });
                }
                else
                {
                    outboundChunks.Enqueue(new HTTPChunk()
                    {
                        Id = chunkId,
                        Data = message.Data.Substring(i, ChunkLength),
                        Length = ChunkLength,
                        Final = false
                    });
                }
            }

            return true;
        }

        public ModuleInformation GetModuleInfo()
        {
            return new ModuleInformation
            {
                Name = "HTTP Comm Module",
                Developer = "Adam Chester, Daniel Duggan",
                Description = "Binds to port 8080, handles command & control over HTTP GET requests."
            };
        }

        protected void AgentHandler(Socket socket)
        {
            // Start another thread for handling this client
            Thread t = new Thread(delegate (object param)
            {
                var agentId = string.Empty;
                var client = (Socket)param;
                var data = new byte[client.ReceiveBufferSize];

                try
                {
                    client.Receive(data);
                    var httpData = Encoding.ASCII.GetString(data);

                    agentId = HandleRequest(httpData);

                    // Response to send to agent
                    var response = GenerateResponse(agentId);

                    client.Send(response);
                    client.Close();
                }
                catch { }
            });

            t.Start(socket);
        }

        protected string HandleRequest(string request)
        {
            var agentId = string.Empty;

            var re = Regex.Match(request, "GET /\\?id=([^&]+)&data=([^\\s]+)");
            if (re.Captures.Count > 0)
            {
                inboundData.Enqueue(new AgentMessage
                {
                    AgentId = agentId = re.Groups[1].Value,
                    Data = re.Groups[2].Value,
                });
            }
            else
            {
                LogWebRequest(request);
            }

            return agentId;
        }

        protected byte[] GenerateResponse(string agentId)
        {
            byte[] response;

            var sessionData = AgentController.GetSession(agentId);
            if (sessionData.datastore.ContainsKey("outboundChunks") == false)
                return Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nmodule=core&command=nop");

            var outboundChunks = (Queue<HTTPChunk>)sessionData.datastore["outboundChunks"];

            if (outboundChunks.Count > 0)
            {
                var outData = outboundChunks.Dequeue();
                response = Encoding.ASCII.GetBytes(string.Format("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nid={0}&data={1}&final={2}", outData.Id, outData.Data, outData.Final));
            }
            else
            {
                response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nmodule=core&command=nop");
            }

            return response;
        }

        private void LogWebRequest(string request)
        {
            Program.ServerController.AddWebLog(new WebLog
            {
                Time = DateTime.UtcNow,
                Request = request.Replace("\r\n", " ").Replace("\0", "").TrimEnd()
            });
        }
    }
}