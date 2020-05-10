using Agent.Common;
using Agent.Controllers;

namespace Agent.Commands
{
    internal class BlockDlls
    {
        public static void SetConfig(string data, AgentController agent, ConfigController config)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (bool.TryParse(data, out bool disable))
                {
                    config.SetOption(Models.ConfigurationSettings.BlockDLLs, disable);
                }
                else
                {
                    agent.SendError("stageone", "blockdlls", "Argument is not of type bool");
                    return;
                }
            }

            var result = (new SharpC2ResultList<BlockDllsSetting>
            {
                new BlockDllsSetting
                {
                    Disabled = (bool)config.GetOption(Models.ConfigurationSettings.BlockDLLs)
                }
            });

            agent.SendCommandOutput(result.ToString());
        }
    }
}