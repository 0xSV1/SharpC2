using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TeamServer.Models
{
    public class ClientAuthenticationRequest
    {
        public string Nick { get; set; }
        public string ServerPassword { get; set; }
    }

    public class ClientAuthenticationResponse
    {
        public AuthenticationStatus AuthenticationResult { get; set; }
        public string AuthenticationToken { get; set; }
        public string SessionToken { get; set; }

        public enum AuthenticationStatus
        {
            Success,
            BadPassword,
            NickInUse
        }
    }

    internal class ConnectedClient
    {
        internal string Nick { get; set; }
        internal string SessionToken { get; set; }
    }

    public class ClientLogoutRequest
    {
        [BindRequired]
        public string Nick { get; set; }

        [BindRequired]
        public string SessionToken { get; set; }
    }
}