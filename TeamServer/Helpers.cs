using System;
using System.Text;
using System.Linq;
using System.Security.Cryptography;

namespace TeamServer
{
    public class Common
    {
        public static string jwtSecret = Helpers.GenerateRandomString(128);
    }

    public class Helpers
    {
        public static string GenerateRandomString(int len)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, len)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string Base64Decode(string input)
        {
            return Encoding.ASCII.GetString(Convert.FromBase64String(input));
        }

        public static string Base64Encode(byte[] input)
        {
            return Convert.ToBase64String(input);
        }

        public static string GetPassword()
        {
            Console.Write("Server Password: ");

            var password = string.Empty;
            var nextKey = Console.ReadKey(true);

            while (nextKey.Key != ConsoleKey.Enter)
            {
                if (nextKey.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password = password.Substring(0, password.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    password += nextKey.KeyChar;
                    Console.Write("*");
                }

                nextKey = Console.ReadKey(true);
            }

            return password;
        }

        public static byte[] GetPasswordHash(string plaintext)
        {
            byte[] hashedPass;

            using (var crypt = SHA256.Create())
                hashedPass = crypt.ComputeHash(Encoding.ASCII.GetBytes(plaintext));

            return hashedPass;
        }
    }
}