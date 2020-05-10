using C2.Models;

using Agent.Controllers;

namespace Agent.Interfaces
{
    public interface ICommModule
    {
        void Initialise(ConfigController configController);
        void Run();
        void SendData(AgentMessage message);
        bool RecvData(out AgentMessage message);
        void Stop();
    }
}