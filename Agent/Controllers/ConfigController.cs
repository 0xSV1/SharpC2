using System.Collections.Generic;

using Agent.Models;

namespace Agent.Controllers
{
    public class ConfigController
    {
        protected Dictionary<ConfigurationSettings, object> configSettings = new Dictionary<ConfigurationSettings, object>();

        public ConfigController Create()
        {
            return new ConfigController();
        }

        public void SetOption(ConfigurationSettings option, object value)
        {
            if (configSettings.ContainsKey(option))
                configSettings[option] = value;
            else
                AddOption(option, value);
        }

        private void AddOption(ConfigurationSettings option, object value)
        {
            configSettings.Add(option, value);
            configSettings[option] = value;
        }

        public object GetOption(ConfigurationSettings option)
        {
            if (configSettings.ContainsKey(option))
                return configSettings[option];
            else
                return null;
        }
    }
}