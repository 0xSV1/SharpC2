namespace Agent.Models
{
    public enum ConfigurationSettings
    {
        AgentId,        // string
        C2Host,         // string
        C2Port,         // int
        SleepTime,      // int
        Jitter,         // int
        PPID,           // int
        BlockDLLs,      // bool
        DisableETW,     // bool
        SpawnTo,        // string
    }
}