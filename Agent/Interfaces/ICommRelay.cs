using C2.Models;

namespace Agent.Interfaces
{
    public interface ICommRelay
    {
        void GarbageIn(AgentMessage message);
        bool GarbageOut(out AgentMessage message);
    }
}