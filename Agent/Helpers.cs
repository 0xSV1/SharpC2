using System;
using System.Linq;

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
    }
}