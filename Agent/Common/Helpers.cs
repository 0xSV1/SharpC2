using System;
using System.Net;
using System.Linq;
using System.Diagnostics;
using System.Security.Principal;

using C2.Models;

namespace Agent
{
    public class Helpers
    {
        public static string GenerateRandomString(int len)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, len)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static byte[] Base64Decode(string data)
        {
            return Convert.FromBase64String(data);
        }

        public static bool Is64BitProcess
        {
            get { return IntPtr.Size == 8; }
        }

        public static string GetProcessIntegrity(WindowsIdentity identity)
        {
            var integrity = "Medium";

            if (Environment.UserName.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase)) { integrity = "SYSTEM"; }
            else if (identity.Owner != identity.User) { integrity = "High"; }

            return integrity;
        }

        public static InitialMetadata GetInitialMetadata(string AgentID)
        {
            var proc = Process.GetCurrentProcess();
            var host = Dns.GetHostName();
            var ips = Dns.GetHostEntry(host).AddressList.Where(i => i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Select(i => i.ToString()).ToArray();
            var identity = WindowsIdentity.GetCurrent();
            return new InitialMetadata
            {
                AgentId = AgentID,
                ComputerName = host,
                InternalAddresses = ips,
                ProcessName = proc.ProcessName,
                ProcessId = proc.Id,
                Identity = identity.Name,
                Architecture = Is64BitProcess ? "x64" : "x86",
                Integrity = GetProcessIntegrity(identity)
            };
        }
    }
}