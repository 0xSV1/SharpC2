using System;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using TeamServer.Modules;
using TeamServer.Controllers;

namespace TeamServer
{
    public class Program
    {
        internal static byte[] ServerPassword;
        internal static ClientController ClientController;
        internal static ServerController ServerController;

        public static void Main(string[] args)
        {
            var password = string.Empty;

            if (args.Length == 1) {
                password = args[0];
            }
            else
            {
                while (string.IsNullOrEmpty(password))
                {
                    password = Helpers.GetPassword();
                    if (string.IsNullOrEmpty(password)) { Console.WriteLine("\r\nPassword cannot be blank\r\n"); }
                }

                ServerPassword = Helpers.GetPasswordHash(password);
            }

            Console.WriteLine("\r\n");
            Console.WriteLine(" ███████ ██   ██  █████  ██████  ██████   ██████ ██████  ");
            Console.WriteLine(" ██      ██   ██ ██   ██ ██   ██ ██   ██ ██           ██ ");
            Console.WriteLine(" ███████ ███████ ███████ ██████  ██████  ██       █████  ");
            Console.WriteLine("      ██ ██   ██ ██   ██ ██   ██ ██      ██      ██      ");
            Console.WriteLine(" ███████ ██   ██ ██   ██ ██   ██ ██       ██████ ███████ ");
            Console.WriteLine("                                                         ");
            Console.WriteLine("                                   Adam Chester (@_xpn_) ");
            Console.WriteLine("                            Daniel Duggan (@_RastaMouse) ");
            Console.WriteLine("\r\n");

            // Used to handle client connections to the server
            ClientController = new ClientController();

            // Used to store session data from agents
            var agentController = AgentController.Create();

            // Used to handle commands
            var serverController = new ServerController(agentController);

            // Load server modules
            var coreModule = new CoreServerModule();
            var rportfwdModule = new PortFwdModule();

            serverController.AddServerModule(coreModule);
            serverController.AddServerModule(rportfwdModule);
            
            serverController.SetCommModule(new HTTPCommModule());
            serverController.Start();

            ServerController = serverController;

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}