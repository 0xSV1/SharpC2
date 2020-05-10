using System;

namespace C2.Models
{
    [Serializable]
    public class AgentMessage
    {
        public string AgentId { get; set; }
        public string Data { get; set; }
    }

    [Serializable]
    public class C2Data
    {
        public string AgentId { get; set; }
        public string Module { get; set; }
        public string Command { get; set; }
        public string Data { get; set; }
    }

    [Serializable]
    public class InitialMetadata
    {
        public string AgentId { get; set; }
        public string ParentAgentId { get; set; }
        public string ComputerName { get; set; }
        public string[] InternalAddresses { get; set; }
        public string Identity { get; set; }
        public string ProcessName { get; set; }
        public int ProcessId { get; set; }
        public string Architecture { get; set; }
        public string Integrity { get; set; }
    }
}