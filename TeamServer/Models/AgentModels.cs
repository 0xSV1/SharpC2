using System;
using System.Collections.Generic;

namespace TeamServer.Models
{
    public class AgentCommandRequest
    {
        public string AgentId { get; set; }
        public string Module { get; set; }
        public string Command { get; set; }
        public string Data { get; set; }
    }

    [Serializable]
    public class AgentModuleInfo
    {
        public string Name { get; set; }
        public string Developer { get; set; }
        public List<AgentCommand> Commands { get; set; }

        [Serializable]
        public class AgentCommand
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string HelpText { get; set; }
            public bool Visible { get; set; }
        }
    }
}