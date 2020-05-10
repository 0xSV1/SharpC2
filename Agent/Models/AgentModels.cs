using System;
using System.Collections.Generic;

using Agent.Controllers;

namespace Agent.Models
{
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
            public bool Visible { get; set; } = true;

            [NonSerialized] private AgentController.OnAgentCommand _callback;

            public AgentController.OnAgentCommand Callback
            {
                get { return _callback; }
                set { _callback = value; }
            }
        }
    }

    public class TcpAgent
    {
        public string Address { get; set; }
        public int Port { get; set; }
    }

    public enum AgentStatus
    {
        Running,
        Stopped
    }
}