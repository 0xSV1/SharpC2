using System;

namespace TeamServer.Models
{
    internal class HTTPChunk
    {
        internal string Id { get; set; }
        internal string Data { get; set; }
        internal int Length { get; set; }
        internal bool Final { get; set; }
    }

    public class WebLog
    {
        public DateTime Time { get; set; }
        public string Request { get; set; }
    }
}