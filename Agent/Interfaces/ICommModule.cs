using Agent.Models;
using Agent.Controllers;

namespace Agent.Interfaces
{
    public interface ICommModule
    {
        void Initialise(ConfigurationController configController);
        void Run();
        void SendData(C2Data c2Data);
        bool RecvData(out C2Data c2Data);
        void Stop();
    }
}