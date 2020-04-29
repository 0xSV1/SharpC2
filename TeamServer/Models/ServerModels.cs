using System.Collections.Generic;

using TeamServer.Controllers;

namespace TeamServer.Models
{
    public class ServerModuleInfo
    {
        public string Name { get; set; }
        public string Developer { get; set; }
        public List<Command> Commands { get; set; }
        public class Command
        {
            public string Name { get; set; }
            public string Description { get; set; }
            internal ServerController.OnServerCommand CallBack { get; set; }
        }
    }

    internal enum ServerControllerStatus
    {
        Started,
        Stopped
    }
}