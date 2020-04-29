using System;
using System.Linq;
using System.Collections.Generic;

using TeamServer.Models;

namespace TeamServer.Controllers
{
    internal class ClientController
    {
        private List<ConnectedClient> connectedClients;
        private event EventHandler<ClientEvent> OnClientEvent;
        private List<ClientEvent> ClientEvents = new List<ClientEvent>();

        internal ClientController()
        {
            connectedClients = new List<ConnectedClient>();
            OnClientEvent += ClientEventHandler;
        }

        internal ClientAuthenticationResponse AuthenticateClient(ClientAuthenticationRequest request)
        {
            var result = new ClientAuthenticationResponse();

            // check password is valid
            if (!ValidateServerPassword(request.ServerPassword)) { result.AuthenticationResult = ClientAuthenticationResponse.AuthenticationStatus.BadPassword; return result; }

            // check nick is not in use
            if (NickInUse(request.Nick)) { result.AuthenticationResult = ClientAuthenticationResponse.AuthenticationStatus.NickInUse; return result; }

            result.AuthenticationResult = ClientAuthenticationResponse.AuthenticationStatus.Success;

            // generate new session token
            var sessionToken = Helpers.GenerateRandomString(6);
            result.SessionToken = sessionToken;

            AddConnectedClient(new ConnectedClient { Nick = request.Nick, SessionToken = sessionToken });
            return result;
        }

        internal string[] GetConnectedClients()
        {
            return connectedClients.Select(c => c.Nick).ToArray();
        }

        internal bool RemoveConnectedClient(ClientLogoutRequest request)
        {
            var client = connectedClients.Where(c => c.Nick.Equals(request.Nick, StringComparison.OrdinalIgnoreCase) && c.SessionToken.Equals(request.SessionToken)).FirstOrDefault();
            var result = connectedClients.Remove(client);
            if (result) { SendEvent(ClientEvent.ClientEventType.UserLogoff, request.Nick); }
            return result;
        }

        internal void SendEvent(ClientEvent.ClientEventType e, string data)
        {
            OnClientEvent?.Invoke(this, new ClientEvent { EventTime = DateTime.UtcNow, EventType = e, Data = data });
        }

        internal ClientEvent[] GetClientEventsSince(DateTime date)
        {
            return ClientEvents.Where(e => e.EventTime > date).ToArray();
        }

        protected bool ValidateServerPassword(string password)
        {
            return Helpers.GetPasswordHash(password).SequenceEqual(Program.ServerPassword);
        }

        protected bool NickInUse(string nick)
        {
            return connectedClients.Select(c => c.Nick).Contains(nick);
        }

        protected void AddConnectedClient(ConnectedClient client)
        {
            connectedClients.Add(client);
            SendEvent(ClientEvent.ClientEventType.UserLogon, client.Nick);
        }

        private void ClientEventHandler(object sender, ClientEvent e)
        {
            ClientEvents.Add(e);
        }
    }
}