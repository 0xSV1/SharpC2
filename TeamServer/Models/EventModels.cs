using System;

namespace TeamServer.Models
{
    public class ClientEvent
    {
        public DateTime EventTime { get; set; }
        public ClientEventType EventType { get; set; }
        public string Data { get; set; }

        public enum ClientEventType
        {
            UserLogon,
            UserLogoff
        }
    }

    public class AgentEvent
    {
        public string AgentId { get; set; }
        public DateTime EventTime { get; set; }
        public AgentEventType EventType { get; set; }
        public string Data { get; set; }

        public enum AgentEventType
        {
            AgentConnected,
            AgentExited,
            AgentCommandResponse,
            AgentModuleRegistered,
            AgentErrorMessage
        }
    }
}