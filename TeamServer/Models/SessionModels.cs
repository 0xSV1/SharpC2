using System;
using System.Collections.Generic;

namespace TeamServer.Models
{
    public class SessionData
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
        public DateTime FirstSeen { get; set; }
        public DateTime LastCheckIn { get; set; }
        
        internal Dictionary<string, object> datastore = new Dictionary<string, object>();
        public List<AgentModuleInfo> loadedModules = new List<AgentModuleInfo>();
    }
}