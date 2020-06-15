using Adriva.Common.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System;
using System.Security.Claims;

namespace Adriva.Web.Core.Authentication
{

    public class GoogleAuthenticationTicketParser : IAuthenticationTicketParser
    {

        public string SchemeName => GoogleDefaults.AuthenticationScheme;

        public AuthenticationSchemeType Scheme => AuthenticationSchemeType.Google;

        public ExternalIdentity Parse(AuthenticationTicket ticket)
        {
            string id = ticket.Principal.GetValue(ClaimTypes.NameIdentifier, null);
            string firstName = ticket.Principal.GetValue(ClaimTypes.GivenName, null);
            string lastName = ticket.Principal.GetValue(ClaimTypes.Surname, null);
            string name = ticket.Principal.GetValue(ClaimTypes.Name, null);
            string email = ticket.Principal.GetValue(ClaimTypes.Email, null);
            string accessToken = ticket.Properties.GetTokenValue("access_token");
            string expiresAtString = ticket.Properties.GetTokenValue("expires_at");

            DateTime.TryParse(expiresAtString, out DateTime expiresAt);

            return new ExternalIdentity(this.Scheme) {
                Id = id,
                AccessToken = accessToken,
                EMail = email,
                ExpiresAt = expiresAt,
                FirstName = firstName,
                LastName = lastName,
                Name = name,
                Username = email,
                ProfilePicture = null
            };
        }
    }
}
