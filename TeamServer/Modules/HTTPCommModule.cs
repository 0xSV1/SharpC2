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
    public class HTTPCommModule : ICommModule
    {
        protected const int ChunkLength = 1024;
        
        protected Queue<C2Data> outboundData = new Queue<C2Data>();
        protected Queue<C2Data> inboundData = new Queue<C2Data>();
        protected Queue<string> outboundChunks = new Queue<string>();

        protected Socket socket;
        protected AgentController agentController;

        public void Initialise(AgentController agentController)
        {
            this.agentController = agentController;
        }

        public void Start()
        {
            // Set up our HTTP socket
            socket = new Socket(SocketType.Stream, ProtocolType.IP);
            socket.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 8080));
            socket.Listen(20);

            // Execute within thread to avoid this call from blocking
            Thread t = new Thread(delegate (object param)
            {
                while (true)
                {
                    Socket clientSocket = socket.Accept();
                    AgentHandler(clientSocket as Socket);
                }
            });

            t.Start(socket);
        }

        public bool RecvData(out C2Data data)
        {
            if (inboundData.Count > 0)
            {
                data = inboundData.Dequeue();
                return true;
            }

            data = null;
            return false;
        }

        public bool SendData(C2Data data)
        {
            var chunkId = Helpers.GenerateRandomString(10);
            var sessionData = agentController.GetSession(data.AgentId);

            // Add "outboundChunks" property if it doesn't exist for this agent
            if (sessionData.datastore.ContainsKey("outboundChunks") == false)
                sessionData.datastore.Add("outboundChunks", new Queue<HTTPChunk>());

            var outboundChunks = sessionData.datastore["outboundChunks"] as Queue<HTTPChunk>;

            for (int i = 0; i <= data.Data.Length; i += ChunkLength)
            {
                if (i + ChunkLength >= data.Data.Length)
                {
                    outboundChunks.Enqueue(new HTTPChunk()
                    {
                        Id = chunkId,
                        Module = data.Module,
                        Command = data.Command,
                        Data = data.Data[i..],
                        Length = data.Data.Length - i,
                        Final = true
                    });
                }
                else
                {
                    outboundChunks.Enqueue(new HTTPChunk()
                    {
                        Id = chunkId,
                        Module = data.Module,
                        Command = data.Command,
                        Data = data.Data.Substring(i, ChunkLength),
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

            var re = System.Text.RegularExpressions.Regex.Match(request, "GET /\\?module=([^&]+)&command=([^&]+)&data=([^&]*)&agentid=([^\\s]+)");
            if (re.Captures.Count > 0)
            {
                inboundData.Enqueue(new C2Data()
                {
                    Module = re.Groups[1].Value,
                    Command = re.Groups[2].Value,
                    Data = re.Groups[3].Value,
                    AgentId = agentId = re.Groups[4].Value
                });
            }
            else
            {
                Program.ServerController.LogWebRequest(request);
            }

            return agentId;
        }

        protected byte[] GenerateResponse(string agentId)
        {
            byte[] response;

            var sessionData = agentController.GetSession(agentId);
            if (sessionData.datastore.ContainsKey("outboundChunks") == false)
                return Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nmodule=core&command=nop");

            var outboundChunks = (Queue<HTTPChunk>)sessionData.datastore["outboundChunks"];

            if (outboundChunks.Count > 0)
            {
                var outData = outboundChunks.Dequeue();
                response = Encoding.ASCII.GetBytes(string.Format("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nmodule={0}&command={1}&data={2}&id={3}&final={4}", outData.Module, outData.Command, outData.Data, outData.Id, outData.Final));
            }
            else
            {
                response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nmodule=core&command=nop");
            }

            return response;
        }
    }
}