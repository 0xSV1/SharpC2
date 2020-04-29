using System;
using System.Text;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

using TeamServer.Models;

namespace TeamServer.ApiControllers
{
    [Authorize]
    [Route("api/clients")]
    public class ClientsController : Controller
    {
        [HttpGet]
        public string[] GetLoggedInUsers()
        {
            return Program.ClientController.GetConnectedClients();
        }

        [HttpGet("events")]
        public ClientEvent[] GetClientEvents(string date)
        {
            ClientEvent[] events;

            if (DateTime.TryParse(date, out DateTime startDate))
                events = Program.ClientController.GetClientEventsSince(startDate);
            else
                events = Program.ClientController.GetClientEventsSince(DateTime.UtcNow.Add(new TimeSpan(-1, 0, 0)));
            
            return events;
        }

        [AllowAnonymous]
        [HttpPost]
        public ClientAuthenticationResponse ClientLogin([FromBody]ClientAuthenticationRequest request)
        {
            var result = Program.ClientController.AuthenticateClient(request);

            if (result.AuthenticationResult != ClientAuthenticationResponse.AuthenticationStatus.Success) { return result; }

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSecret = Encoding.ASCII.GetBytes(Common.jwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, request.Nick) }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(jwtSecret), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            result.AuthenticationToken = tokenHandler.WriteToken(token);

            return result;
        }

        [HttpDelete]
        public IActionResult ClientLogout(ClientLogoutRequest request)
        {
            var success = Program.ClientController.RemoveConnectedClient(request);
            if (success) { return Ok(); } else { return BadRequest(); }
        }
    }
}