using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace C2
{
    public static class Helpers
    {
        public static byte[] Serialise<T>(T data)
        {
            using (var ms = new MemoryStream())
            {
                var ser = new DataContractJsonSerializer(typeof(T));
                ser.WriteObject(ms, data);
                return ms.ToArray();
            }
        }

        public static T Deserialise<T>(string data)
        {
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(data)))
            {
                var ser = new DataContractJsonSerializer(typeof(T));
                return (T)ser.ReadObject(ms);
            }
        }

        public static T Deserialise<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                var ser = new DataContractJsonSerializer(typeof(T));
                return (T)ser.ReadObject(ms);
            }
        }

        public static byte[] Prune(this byte[] bytes)
        {
            if (bytes.Length == 0) return bytes;
            var i = bytes.Length - 1;
            while (bytes[i] == 0)
            {
                i--;
            }
            byte[] copy = new byte[i + 1];
            Array.Copy(bytes, copy, i + 1);
            return copy;
        }
    }
}