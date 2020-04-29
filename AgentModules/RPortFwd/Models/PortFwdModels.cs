using System.Net;
using System.Net.Sockets;

namespace Agent.Models
{
    internal class ReversePortForward
    {
        internal IPAddress BindAddress { get; set; }
        internal int BindPort { get; set; }
        internal IPAddress ForwardAddress { get; set; }
        internal int ForwardPort { get; set; }

        internal Socket Socket { get; set; }
    }
}