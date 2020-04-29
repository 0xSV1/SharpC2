namespace TeamServer.Models
{
    internal class HTTPChunk
    {
        internal string Id { get; set; }
        internal string Data { get; set; }
        internal int Length { get; set; }
        internal bool Final { get; set; }
        internal string Module { get; set; }
        internal string Command { get; set; }
    }
}